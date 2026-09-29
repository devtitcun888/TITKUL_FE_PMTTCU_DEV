using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class HocLieuModel : PageModel
{
    private readonly BackendApiClient _api;
    public HocLieuModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<AlbumItem> Items { get; private set; } = [];
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? q, int page = 1)
    {
        page = Math.Max(1, page);
        Query = q?.Trim();
        var path = $"/api/v1/public/albums?kind=HOC_LIEU&page={page}&pageSize=20";
        if (!string.IsNullOrWhiteSpace(Query)) path += "&q=" + Uri.EscapeDataString(Query);
        var list = await _api.GetPublicJsonAsync<ListEnvelope<AlbumItem>>(path);
        if (list is null) ErrorMessage = "Chưa thể tải danh sách học liệu. Vui lòng thử lại sau.";
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? page;
        PageSize = list?.PageSize ?? 20;
        Total = list?.Total ?? Items.Count;
        ViewData["Title"] = "Học liệu số";
        ViewData["Description"] = "Tìm và tải học liệu dùng chung của Trung tâm; học liệu riêng từng lớp nằm trong trang lớp tương ứng.";
        ViewData["CanonicalPath"] = "/hoc-lieu";
    }

    public sealed record AlbumItem(string Title, string Slug, string? Summary);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
