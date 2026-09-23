using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class HocLieuChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public HocLieuChiTietModel(BackendApiClient api) => _api = api;
    public AlbumHead? Item { get; private set; }
    public IReadOnlyList<MediaItem> Media { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var body = await _api.GetPublicJsonAsync<DetailEnvelope>("/api/v1/public/albums/" + slug);
        if (body?.Item is null || body.Item.Kind != "HOC_LIEU")
        {
            ErrorMessage = "Không tìm thấy học liệu.";
            return Page();
        }

        Item = body.Item;
        Media = body.Media ?? [];
        ViewData["Title"] = Item.Title;
        return Page();
    }

    public async Task<IActionResult> OnGetTaiAsync(string slug, Guid id)
    {
        var file = await _api.GetFileAsync("/api/v1/public/files/media/" + id);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, file.ContentType ?? "application/octet-stream", file.FileName ?? "hoc-lieu");
    }

    public sealed record MediaItem(string Kind, string? Title, Guid? FileId);
    public sealed record AlbumHead(string Title, string Slug, string Kind, string? Summary);
    private sealed record DetailEnvelope(AlbumHead? Item, IReadOnlyList<MediaItem>? Media);
}
