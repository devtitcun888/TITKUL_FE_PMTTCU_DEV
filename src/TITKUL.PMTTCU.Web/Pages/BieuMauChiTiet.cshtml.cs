using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class BieuMauChiTietModel(BackendApiClient api) : PageModel
{
    public PublicForm? Item { get; private set; }
    public bool CanPreview => string.Equals(Item?.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase) && Item?.ViewMode != "DOWNLOAD_ONLY" && CanDownload;
    public bool CanDownload => IsWithinValidity(Item?.EffectiveFrom, Item?.ExpiresOn);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var result = await api.GetPublicJsonResultAsync<FormEnvelope>($"/api/v1/public/forms/{id}");
        if (result.IsNotFound) return NotFound();
        if (!result.IsAvailable) return StatusCode(StatusCodes.Status503ServiceUnavailable);
        Item = result.Value?.Item;
        if (Item is null) return NotFound();
        ViewData["Title"] = Item.Title;
        return Page();
    }

    public async Task<IActionResult> OnGetXemAsync(Guid id)
    {
        var result = await api.GetPublicJsonResultAsync<FormEnvelope>($"/api/v1/public/forms/{id}");
        var item = result.Value?.Item;
        if (item?.MimeType != "application/pdf" || item.ViewMode == "DOWNLOAD_ONLY") return NotFound();
        if (HostFile.RedirectIfPublic(item.FileUrl) is { } jump) return jump;
        var file = await api.GetFileResultAsync($"/api/v1/public/files/forms/{id}?inline=true");
        if (HostFile.RedirectIfPublic(file.RedirectUrl) is { } redirect) return redirect;
        if (!file.IsSuccess) return file.IsNotFound ? NotFound() : StatusCode(503);
        var bytes = file.Bytes!;
        if (bytes.Length < 5 || bytes[0] != 0x25 || bytes[1] != 0x50 || bytes[2] != 0x44 || bytes[3] != 0x46 || bytes[4] != 0x2D) return StatusCode(415);
        Response.Headers["Content-Disposition"] = "inline";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, no-store";
        return new FileContentResult(bytes, "application/pdf") { EnableRangeProcessing = true };
    }

    private static bool IsWithinValidity(DateOnly? from, DateOnly? until)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        return (from is null || from.Value <= today) && (until is null || until.Value >= today);
    }

    public sealed record PublicForm(Guid Id, string? Code, string Title, string? Summary, string FileName, DateTimeOffset? PublishedAt, DateOnly? EffectiveFrom, DateOnly? ExpiresOn, string MimeType, string ViewMode, string? FileUrl = null);
    private sealed record FormEnvelope(PublicForm? Item);
}
