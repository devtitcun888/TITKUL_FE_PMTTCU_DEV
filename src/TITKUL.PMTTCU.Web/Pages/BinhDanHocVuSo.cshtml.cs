using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class BinhDanHocVuSoModel : PageModel
{
    private readonly BackendApiClient _api;

    public BinhDanHocVuSoModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<PostItem> Items { get; private set; } = [];
    public IReadOnlyList<CategoryItem> Categories { get; private set; } = [];
    public string? CategorySlug { get; private set; }
    public string CategoryTitle { get; private set; } = "Bình dân học vụ số";
    public string? ErrorMessage { get; private set; }
    public string? Query { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 12;
    public int Total { get; private set; }

    public async Task OnGetAsync(string? q, string? categorySlug, int page = 1)
    {
        CurrentPage = Math.Max(1, page);
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var categories = await _api.GetPublicJsonAsync<CategoryEnvelope>("/api/v1/public/categories?kind=HUONG_DAN_SO");
        Categories = (categories?.Items ?? []).Where(item => item.Active).OrderBy(item => item.Sort).ToArray();
        var selected = Categories.FirstOrDefault(item => item.Slug == categorySlug);
        CategorySlug = selected?.Slug;
        if (selected is not null) CategoryTitle = selected.Name;
        var selectedSlug = CategorySlug ?? "binh-dan-hoc-vu-so";
        var path = $"/api/v1/public/posts?page={CurrentPage}&pageSize={PageSize}&kind=HUONG_DAN_SO&categorySlug={Uri.EscapeDataString(selectedSlug)}";
        if (Query is not null) path += "&q=" + Uri.EscapeDataString(Query);

        var response = await _api.GetPublicJsonAsync<ListEnvelope<PostItem>>(path);
        if (response is null) ErrorMessage = "Chưa thể tải nội dung Bình dân học vụ số. Vui lòng thử lại sau.";
        Items = response?.Items ?? [];
        CurrentPage = response?.Page ?? CurrentPage;
        PageSize = response?.PageSize ?? PageSize;
        Total = response?.Total ?? Items.Count;
    }

    public sealed record PostItem(string Title, string Slug, string? Summary, DateTimeOffset? PublishedAt, string CategoryName, string? CoverUrl, string? ThumbnailUrl = null);
    public sealed record CategoryItem(Guid Id, string Kind, string Name, string Slug, string? Description, int Sort, bool Active);
    private sealed record CategoryEnvelope(IReadOnlyList<CategoryItem>? Items);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
