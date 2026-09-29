using Microsoft.AspNetCore.WebUtilities;

namespace TITKUL.PMTTCU.Web.Observability;

public sealed class PublicVisitCookieMiddleware(RequestDelegate next)
{
    public const string CookieName = "pmttcu.public-session";

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var acceptsHtml = request.Headers["Accept"].Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
        var path = request.Path;
        var isPublicPage = HttpMethods.IsGet(request.Method) && acceptsHtml &&
            !path.StartsWithSegments("/admin") && !path.StartsWithSegments("/api") &&
            !path.StartsWithSegments("/health") && !Path.HasExtension(path.Value);
        var trackingAllowed = request.Headers["DNT"] != "1" && request.Headers["Sec-GPC"] != "1";

        if (isPublicPage && trackingAllowed && !request.Cookies.ContainsKey(CookieName))
        {
            var value = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            context.Response.Cookies.Append(CookieName, value, new CookieOptions
            {
                HttpOnly = true,
                Secure = request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = false
            });
        }

        await next(context);
    }
}
