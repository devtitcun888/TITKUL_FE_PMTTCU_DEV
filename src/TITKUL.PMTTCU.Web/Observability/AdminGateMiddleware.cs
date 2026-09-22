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
        var isLogin = path.StartsWithSegments("/admin/dang-nhap");
        if (isAdmin && !isLogin)
        {
            var token = context.Request.Cookies[CookieName];
            if (string.IsNullOrWhiteSpace(token) || !await api.HasSessionAsync(token))
            {
                context.Response.Redirect("/admin/dang-nhap");
                return;
            }
        }

        await _next(context);
    }
}
