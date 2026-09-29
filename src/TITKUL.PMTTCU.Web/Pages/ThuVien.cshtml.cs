using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class ThuVienModel : PageModel
{
    private readonly BackendApiClient _api;
    public ThuVienModel(BackendApiClient api) => _api = api;
    public string Kind { get; private set; } = "IMAGE";
    public IReadOnlyList<AlbumItem> Items { get; private set; } = [];
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? loai, string? q, int page = 1)
    {
        page = Math.Max(1, page);
        Kind = loai is "VIDEO_LINK" ? "VIDEO_LINK" : "IMAGE";
        Query = q?.Trim();
        var path = $"/api/v1/public/albums?kind={Kind}&page={page}&pageSize=20";
        if (!string.IsNullOrWhiteSpace(Query)) path += "&q=" + Uri.EscapeDataString(Query);
        var list = await _api.GetPublicJsonAsync<ListEnvelope<AlbumItem>>(path);
        if (list is null) ErrorMessage = "Chưa thể tải thư viện. Vui lòng thử lại sau.";
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? page;
        PageSize = list?.PageSize ?? 20;
        Total = list?.Total ?? Items.Count;
        ViewData["Title"] = Kind == "VIDEO_LINK" ? "Video hoạt động" : "Hình ảnh hoạt động";
        ViewData["Description"] = Kind == "VIDEO_LINK" ? "Video hoạt động, lớp học và sinh hoạt cộng đồng của Trung tâm." : "Hình ảnh hoạt động, lớp học và sinh hoạt cộng đồng của Trung tâm.";
    }

    public sealed record AlbumItem(string Title, string Slug, string? Summary);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
