using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TITKUL.PMTTCU.Web;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Health;
using TITKUL.PMTTCU.Web.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 25 * 1024 * 1024;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddTransient<CorrelationForwardingHandler>();
builder.Services.AddScoped<PublicSiteConfigProvider>();
builder.Services.AddScoped<PublicUrgentNoticeProvider>();
builder.Services.AddScoped<PublicNavigationProvider>();
builder.Services.AddHttpClient<BackendApiClient>(client =>
{
    var baseUrl = builder.Configuration["Backend:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }

    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
    .AddHttpMessageHandler<CorrelationForwardingHandler>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = builder.Configuration.GetValue<long?>("Http:MaxRequestBodyBytes") ?? 25 * 1024 * 1024;
});

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<PublicVisitCookieMiddleware>();
app.UseMiddleware<AdminGateMiddleware>();
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    var inlinePdf = context.Request.Path.StartsWithSegments("/van-ban/pdf", StringComparison.OrdinalIgnoreCase)
        || string.Equals(context.Request.Query["handler"], "Xem", StringComparison.OrdinalIgnoreCase);
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = inlinePdf ? "SAMEORIGIN" : "DENY";
    headers["Content-Security-Policy"] =
        $"default-src 'self'; connect-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data: blob: {HostFile.Origin}; media-src 'self' {HostFile.Origin}; frame-src 'self' {HostFile.Origin} https://www.google.com https://maps.google.com https://www.google.com.vn https://www.youtube-nocookie.com https://www.facebook.com; object-src 'none'; base-uri 'self'; frame-ancestors {(inlinePdf ? "'self'" : "'none'")}; form-action 'self'";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.MapPost("/public-visit", async (HttpContext context, BackendApiClient api) =>
{
    var bodyLimit = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
    if (bodyLimit is { IsReadOnly: false }) bodyLimit.MaxRequestBodySize = 256;
    if (context.Request.ContentLength is > 256) return Results.NoContent();
    if (context.Request.Headers["Sec-Fetch-Site"] == "cross-site") return Results.NoContent();
    var origin = context.Request.Headers.Origin.ToString();
    if (!string.IsNullOrWhiteSpace(origin) && Uri.TryCreate(origin, UriKind.Absolute, out var originUri) &&
        !string.Equals(originUri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
        return Results.NoContent();

    if (context.Request.Headers["DNT"] == "1" || context.Request.Headers["Sec-GPC"] == "1" ||
        context.Request.Cookies.TryGetValue(PublicVisitCookieMiddleware.CookieName, out var session) is false ||
        string.IsNullOrWhiteSpace(session)) return Results.NoContent();

    PublicVisitRequest? body;
    try
    {
        body = await context.Request.ReadFromJsonAsync<PublicVisitRequest>();
    }
    catch (Microsoft.AspNetCore.Http.BadHttpRequestException)
    {
        return Results.NoContent();
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.NoContent();
    }

    var sessionHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(session)))
        .ToLowerInvariant();
    using var response = await api.PostPublicJsonAsync("/api/v1/public/visits", new { sessionHash, pageView = body?.PageView ?? false });
    return Results.NoContent();
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthJsonWriter.WriteAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthJsonWriter.WriteAsync
});

app.Run();

public sealed record PublicVisitRequest(bool PageView);
