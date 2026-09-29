using Microsoft.AspNetCore.Http;

namespace TITKUL.PMTTCU.Web.Pages;

internal static class PublicPrivatePageHeaders
{
    public static void Apply(HttpResponse response)
    {
        response.Headers["Cache-Control"] = "no-store, private, max-age=0";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Expires"] = "0";
        response.Headers["Surrogate-Control"] = "no-store";
        response.Headers["Referrer-Policy"] = "no-referrer";
    }
}
