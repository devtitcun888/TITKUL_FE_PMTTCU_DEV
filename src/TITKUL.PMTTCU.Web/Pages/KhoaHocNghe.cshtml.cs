using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class KhoaHocNgheModel : PageModel
{
    private const string CategoryPrefix = "khoa-hoc-nghe-";
    private readonly BackendApiClient _api;

    public KhoaHocNgheModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<CategoryItem> Categories { get; private set; } = [];
    public IReadOnlyList<ContentFeed> Feeds { get; private set; } = [];
    public IReadOnlyList<PostItem> Items { get; private set; } = [];
    public CategoryItem? SelectedCategory { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Query { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 12;
    public int Total { get; private set; }
    public bool IsGroupPage => SelectedCategory is not null;

    public async Task<IActionResult> OnGetAsync(string? group, string? q, int page = 1)
    {
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        CurrentPage = Math.Max(1, page);
        var categoryResponse = await _api.GetPublicJsonAsync<CategoryEnvelope>("/api/v1/public/categories");
        if (categoryResponse is null)
        {
            ErrorMessage = "Chưa thể tải danh mục khóa học nghề. Vui lòng thử lại sau.";
            return Page();
        }

        Categories = (categoryResponse.Items ?? [])
            .Where(item => item.Active && item.Kind == "TIN_TUC" && item.Slug.StartsWith(CategoryPrefix, StringComparison.Ordinal))
            .OrderBy(item => item.Sort)
            .ThenBy(item => item.Name, StringComparer.CurrentCulture)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(group))
        {
            var slug = CategoryPrefix + group.Trim().ToLowerInvariant();
            SelectedCategory = Categories.FirstOrDefault(item => item.Slug == slug);
            if (SelectedCategory is null) return NotFound();
            var result = await FetchPostsAsync(SelectedCategory.Slug, CurrentPage, PageSize, Query);
            if (result is null) ErrorMessage = "Chưa thể tải bài viết trong nhóm nghề này.";
            Items = result?.Items ?? [];
            CurrentPage = result?.Page ?? CurrentPage;
            PageSize = result?.PageSize ?? PageSize;
            Total = result?.Total ?? Items.Count;
            return Page();
        }

        var feeds = await Task.WhenAll(Categories.Select(async category =>
        {
            var result = await FetchPostsAsync(category.Slug, 1, 4, Query);
            return result is null ? null : new ContentFeed(category, result.Items ?? []);
        }));
        Feeds = feeds.Where(feed => feed is not null && feed.Items.Count > 0).Select(feed => feed!).ToArray();
        if (Categories.Count > 0 && feeds.All(feed => feed is null)) ErrorMessage = "Chưa thể tải nội dung khóa học nghề.";
        return Page();
    }

    private Task<ListEnvelope<PostItem>?> FetchPostsAsync(string categorySlug, int page, int pageSize, string? query)
    {
        var path = $"/api/v1/public/posts?page={page}&pageSize={pageSize}&categorySlug={Uri.EscapeDataString(categorySlug)}";
        if (query is not null) path += "&q=" + Uri.EscapeDataString(query);
        return _api.GetPublicJsonAsync<ListEnvelope<PostItem>>(path);
    }

    public static string GroupPath(CategoryItem category) => "/khoa-hoc-nghe/" + Uri.EscapeDataString(category.Slug[CategoryPrefix.Length..]);

    public sealed record CategoryItem(Guid Id, string Kind, string Name, string Slug, string? Description, int Sort, bool Active);
    public sealed record PostItem(string Title, string Slug, string? Summary, DateTimeOffset? PublishedAt, string CategoryName, string? CoverUrl, string? ThumbnailUrl = null);
    public sealed record ContentFeed(CategoryItem Category, IReadOnlyList<PostItem> Items);
    private sealed record CategoryEnvelope(IReadOnlyList<CategoryItem>? Items);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
