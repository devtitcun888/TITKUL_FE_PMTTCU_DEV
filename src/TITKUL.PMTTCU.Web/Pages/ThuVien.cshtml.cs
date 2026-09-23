using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class ThuVienModel : PageModel
{
    private readonly BackendApiClient _api;
    public ThuVienModel(BackendApiClient api) => _api = api;
    public string Kind { get; private set; } = "IMAGE";
    public IReadOnlyList<AlbumItem> Items { get; private set; } = [];

    public async Task OnGetAsync(string? loai)
    {
        Kind = loai is "VIDEO_LINK" ? "VIDEO_LINK" : "IMAGE";
        var list = await _api.GetPublicJsonAsync<ListEnvelope<AlbumItem>>("/api/v1/public/albums?kind=" + Kind + "&pageSize=20");
        Items = list?.Items ?? [];
        ViewData["Title"] = Kind == "VIDEO_LINK" ? "Video" : "Hình ảnh";
    }

    public sealed record AlbumItem(string Title, string Slug, string? Summary);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
