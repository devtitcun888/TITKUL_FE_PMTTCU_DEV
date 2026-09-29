using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class TinTucModel : PageModel
{
    private readonly BackendApiClient _api;
    public TinTucModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<PostItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? Query { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }

    public async Task OnGetAsync(string? q, int page = 1)
    {
        page = Math.Max(1, page);
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var path = $"/api/v1/public/posts?page={page}&pageSize=20";
        if (Query is not null) path += "&q=" + Uri.EscapeDataString(Query);
        var list = await _api.GetPublicJsonAsync<ListEnvelope<PostItem>>(path);
        if (list is null) ErrorMessage = "Chưa thể tải danh sách tin tức. Vui lòng thử lại sau.";
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? page;
        PageSize = list?.PageSize ?? 20;
        Total = list?.Total ?? Items.Count;
    }

    public sealed record PostItem(string Title, string Slug, string? Summary, DateTimeOffset? PublishedAt, string CategoryName, string? CoverUrl, string? ThumbnailUrl = null);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
