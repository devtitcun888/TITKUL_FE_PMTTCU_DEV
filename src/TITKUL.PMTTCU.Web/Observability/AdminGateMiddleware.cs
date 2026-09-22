using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Observability;

public sealed class AdminGateMiddleware
{
    public const string CookieName = "pmttcu.session";

    private readonly RequestDelegate _next;

    public AdminGateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, BackendApiClient api)
    {
        var path = context.Request.Path;
        var isAdmin = path.StartsWithSegments("/admin");
        var isPublicAdmin = path.StartsWithSegments("/admin/dang-nhap") || path.StartsWithSegments("/admin/dang-xuat");
        if (isAdmin && !isPublicAdmin)
        {
            var token = context.Request.Cookies[CookieName];
            var profile = string.IsNullOrWhiteSpace(token) ? null : await api.GetProfileAsync(token);
            if (profile is null)
            {
                context.Response.Redirect("/admin/dang-nhap");
                return;
            }

            context.Items["StaffProfile"] = profile;
        }

        await _next(context);
    }
}
