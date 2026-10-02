using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class VanBanChiTietModel(BackendApiClient api) : PageModel
{
    public PublicDocument? Item { get; private set; }
    public bool IsPdf => string.Equals(Item?.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    public bool CanDownload => IsWithinValidity(Item?.EffectiveFrom, Item?.ExpiresOn);
    public bool CanPreview => IsPdf && Item?.ViewMode != "DOWNLOAD_ONLY" && CanDownload;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var result = await api.GetPublicJsonResultAsync<DocumentEnvelope>($"/api/v1/public/documents/{id}");
        if (result.IsNotFound) return NotFound();
        if (!result.IsAvailable) return StatusCode(StatusCodes.Status503ServiceUnavailable);

        Item = result.Value?.Item;
        return Item is null ? NotFound() : Page();
    }

    private static bool IsWithinValidity(DateOnly? from, DateOnly? until)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        return (from is null || from.Value <= today) && (until is null || until.Value >= today);
    }

    public sealed record PublicDocument(Guid Id, string? Symbol, string Title, string? Issuer, DateOnly? IssuedOn, string? Field, string FileName, DateOnly? EffectiveFrom = null, DateOnly? ExpiresOn = null, string MimeType = "application/octet-stream", string ViewMode = "AUTO", string? FileUrl = null);
    private sealed record DocumentEnvelope(PublicDocument? Item);
}
