using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class ThuVienChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public ThuVienChiTietModel(BackendApiClient api) => _api = api;
    public AlbumHead? Item { get; private set; }
    public IReadOnlyList<MediaItem> Media { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool ResourceNotFound { get; private set; }

    public async Task OnGetAsync(string slug)
    {
        var result = await _api.GetPublicJsonResultAsync<DetailEnvelope>("/api/v1/public/albums/" + Uri.EscapeDataString(slug));
        var body = result.Value;
        if (body?.Item is null || body.Item.Kind is not ("IMAGE" or "VIDEO_LINK"))
        {
            var notFound = result.IsNotFound || result.IsAvailable;
            ResourceNotFound = notFound;
            ErrorMessage = notFound ? "Không tìm thấy album." : "Chưa thể tải album. Vui lòng thử lại sau.";
            Response.StatusCode = notFound ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
            return;
        }

        Item = body.Item;
        Media = body.Media ?? [];
        ViewData["Title"] = Item.Title;
        ViewData["Description"] = Item.Summary;
        ViewData["CanonicalPath"] = "/thu-vien/" + Uri.EscapeDataString(Item.Slug);
    }

    public async Task<IActionResult> OnGetAnhAsync(string slug, Guid id)
    {
        var file = await _api.GetFileResultAsync("/api/v1/public/files/media/" + id);
        if (!file.IsSuccess) return file.IsNotFound ? NotFound() : StatusCode(503);
        return File(file.Bytes!, file.ContentType ?? "application/octet-stream", file.FileName ?? "anh");
    }

    public sealed record MediaItem(string Kind, string? Title, string? AltText, string? ExternalUrl, Guid? FileId, string? MimeType = null, long? Size = null);
    public sealed record AlbumHead(string Title, string Slug, string Kind, string? Summary);
    private sealed record DetailEnvelope(AlbumHead? Item, IReadOnlyList<MediaItem>? Media);
}
