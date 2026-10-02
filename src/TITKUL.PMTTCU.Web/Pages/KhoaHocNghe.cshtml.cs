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
    public IReadOnlyList<ProgramItem> Programs { get; private set; } = [];
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
        var categoryResponse = await _api.GetPublicJsonAsync<CategoryEnvelope>("/api/v1/public/program-categories");
        if (categoryResponse is null)
        {
            ErrorMessage = "Chưa thể tải danh mục khóa học nghề. Vui lòng thử lại sau.";
            return Page();
        }

        Categories = (categoryResponse.Items ?? [])
            .Where(item => item.Active && item.Kind == "CHUONG_TRINH_HOC")
            .OrderBy(item => item.Sort)
            .ThenBy(item => item.Name, StringComparer.CurrentCulture)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(group))
        {
            var normalized = group.Trim().ToLowerInvariant();
            SelectedCategory = Categories.FirstOrDefault(item => item.Slug == CategoryPrefix + normalized || item.Slug == normalized);
            if (SelectedCategory is null) return NotFound();
            var result = await FetchPostsAsync(SelectedCategory.Slug, CurrentPage, PageSize, Query);
            Items = result?.Items ?? [];
            CurrentPage = result?.Page ?? CurrentPage;
            PageSize = result?.PageSize ?? PageSize;
            Total = result?.Total ?? Items.Count;
            Programs = FilterPrograms(await FetchProgramsAsync(SelectedCategory.Id));
            if (result is null && Programs.Count == 0) ErrorMessage = "Chưa thể tải nội dung nhóm khóa học này.";
            return Page();
        }

        var feeds = await Task.WhenAll(Categories.Select(async category =>
        {
            var result = await FetchPostsAsync(category.Slug, 1, 4, Query);
            var programs = await FetchProgramsAsync(category.Id);
            return new ContentFeed(category, FilterPrograms(programs), result?.Items ?? []);
        }));
        Feeds = feeds.Where(feed => feed is not null && (feed.Items.Count > 0 || feed.Programs.Count > 0)).Select(feed => feed!).ToArray();
        if (Categories.Count > 0 && feeds.All(feed => feed is null)) ErrorMessage = "Chưa thể tải nội dung khóa học nghề.";
        return Page();
    }

    private Task<ListEnvelope<PostItem>?> FetchPostsAsync(string categorySlug, int page, int pageSize, string? query)
    {
        var path = $"/api/v1/public/posts?page={page}&pageSize={pageSize}&kind=CHUONG_TRINH_HOC&categorySlug={Uri.EscapeDataString(categorySlug)}";
        if (query is not null) path += "&q=" + Uri.EscapeDataString(query);
        return _api.GetPublicJsonAsync<ListEnvelope<PostItem>>(path);
    }

    private async Task<IReadOnlyList<ProgramItem>> FetchProgramsAsync(Guid categoryId)
    {
        var response = await _api.GetPublicJsonAsync<ListEnvelope<ProgramItem>>("/api/v1/public/programs?categoryId=" + categoryId);
        return response?.Items ?? [];
    }

    private IReadOnlyList<ProgramItem> FilterPrograms(IReadOnlyList<ProgramItem> programs) =>
        Query is null ? programs : programs.Where(item =>
            item.Name.Contains(Query, StringComparison.CurrentCultureIgnoreCase)
            || (item.Summary?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToArray();

    public static string GroupPath(CategoryItem category) => GroupPath(category.Slug);

    public static string GroupPath(string slug) => "/khoa-hoc-nghe/" + Uri.EscapeDataString(
        slug.StartsWith(CategoryPrefix, StringComparison.Ordinal) ? slug[CategoryPrefix.Length..] : slug);

    public sealed record CategoryItem(Guid Id, string Kind, string Name, string Slug, string? Description, int Sort, bool Active);
    public sealed record PostItem(string Title, string Slug, string? Summary, DateTimeOffset? PublishedAt, string CategoryName, string? CoverUrl, string? ThumbnailUrl = null);
    public sealed record ProgramItem(Guid Id, string Code, string Name, string Slug, Guid CategoryId, string CategoryName, string? Summary, string? ContentHtml, string? CoverUrl, string? SeoTitle, string? SeoDescription, IReadOnlyList<OpenClassItem>? OpenClasses = null);
    public sealed record OpenClassItem(string Code, string Name, string ProgramName, DateOnly StartDate, DateOnly EndDate, int Capacity, int Remaining, string Status, bool CanRegister, string? ClosedReason);
    public sealed record ContentFeed(CategoryItem Category, IReadOnlyList<ProgramItem> Programs, IReadOnlyList<PostItem> Items);
    private sealed record CategoryEnvelope(IReadOnlyList<CategoryItem>? Items);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
