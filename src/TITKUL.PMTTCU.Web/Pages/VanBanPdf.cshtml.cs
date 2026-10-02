using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class VanBanPdfModel(BackendApiClient api) : PageModel
{
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var file = await api.GetFileResultAsync("/api/v1/public/files/documents/" + id + "?inline=true");
        if (HostFile.RedirectIfPublic(file.RedirectUrl) is { } redirect) return redirect;
        if (!file.IsSuccess) return file.IsNotFound ? NotFound() : StatusCode(StatusCodes.Status503ServiceUnavailable);
        var bytes = file.Bytes!;
        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || !HasPdfSignature(bytes))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType);

        Response.Headers["Content-Disposition"] = "inline";
        Response.Headers["Cache-Control"] = "private, no-store";
        return new FileContentResult(bytes, "application/pdf") { EnableRangeProcessing = true };
    }

    private static bool HasPdfSignature(byte[] bytes) =>
        bytes.Length >= 5
        && bytes[0] == 0x25
        && bytes[1] == 0x50
        && bytes[2] == 0x44
        && bytes[3] == 0x46
        && bytes[4] == 0x2D;
}
