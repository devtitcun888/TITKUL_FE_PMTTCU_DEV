using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class TinTucModel : PageModel
{
    private readonly BackendApiClient _api;
    public TinTucModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<PostItem> Items { get; private set; } = [];
    public IReadOnlyList<CategoryItem> Categories { get; private set; } = [];
    public CategoryItem? SelectedCategory { get; private set; }
    public IndexModel.PublicVisitStats? VisitStats { get; private set; }
    public bool CategoriesUnavailable { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Query { get; private set; }
    public string? CategorySlug { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? q, string? categorySlug, int page = 1)
    {
        CurrentPage = Math.Max(1, page);
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var categoriesTask = _api.GetPublicJsonAsync<CategoryEnvelope>("/api/v1/public/categories");
        var statsTask = _api.GetPublicJsonAsync<VisitStatsEnvelope>("/api/v1/public/visit-stats");
        await Task.WhenAll(categoriesTask, statsTask);

        var categoryResponse = await categoriesTask;
        CategoriesUnavailable = categoryResponse is null;
        Categories = (categoryResponse?.Items ?? [])
            .Where(item => item.Active && item.Kind == "TIN_TUC")
            .OrderBy(item => item.Sort)
            .ThenBy(item => item.Name, StringComparer.CurrentCulture)
            .ToArray();
        VisitStats = (await statsTask)?.Item;

        CategorySlug = string.IsNullOrWhiteSpace(categorySlug) ? null : categorySlug.Trim();
        if (CategorySlug is not null && categoryResponse is not null)
        {
            SelectedCategory = Categories.FirstOrDefault(item => string.Equals(item.Slug, CategorySlug, StringComparison.OrdinalIgnoreCase));
            if (SelectedCategory is null) return NotFound();
            CategorySlug = SelectedCategory.Slug;
        }

        var path = $"/api/v1/public/posts?page={CurrentPage}&pageSize=20";
        if (CategorySlug is not null) path += "&categorySlug=" + Uri.EscapeDataString(CategorySlug);
        if (Query is not null) path += "&q=" + Uri.EscapeDataString(Query);
        var list = await _api.GetPublicJsonAsync<ListEnvelope<PostItem>>(path);
        if (list is null) ErrorMessage = "Chưa thể tải danh sách tin tức. Vui lòng thử lại sau.";
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? CurrentPage;
        PageSize = list?.PageSize ?? PageSize;
        Total = list?.Total ?? Items.Count;
        return Page();
    }

    public sealed record PostItem(string Title, string Slug, string? Summary, DateTimeOffset? PublishedAt, string CategoryName, string? CoverUrl, string? ThumbnailUrl = null);
    public sealed record CategoryItem(Guid Id, string Kind, string Name, string Slug, string? Description, int Sort, bool Active);
    private sealed record CategoryEnvelope(IReadOnlyList<CategoryItem>? Items);
    private sealed record VisitStatsEnvelope(IndexModel.PublicVisitStats? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}