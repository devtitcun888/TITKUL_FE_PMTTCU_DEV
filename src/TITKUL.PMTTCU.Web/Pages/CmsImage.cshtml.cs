using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class CmsImageModel(BackendApiClient api) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string token)
    {
        var file = await api.GetFileResultAsync("/api/v1/public/cms-images/" + Uri.EscapeDataString(token));
        if (!file.IsSuccess) return file.IsNotFound ? NotFound() : StatusCode(503);
        if (file.ContentType is not ("image/png" or "image/jpeg")) return NotFound();
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(file.Bytes!, file.ContentType);
    }
}
