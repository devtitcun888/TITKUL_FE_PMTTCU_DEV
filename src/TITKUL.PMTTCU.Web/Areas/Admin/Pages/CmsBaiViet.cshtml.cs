using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsBaiVietModel : PageModel
{
    private static readonly int[] PageSizes = [10, 20, 50];
    private readonly BackendApiClient _api;

    public CmsBaiVietModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<PostItem> Items { get; private set; } = [];
    public IReadOnlyList<CategoryOption> Categories { get; private set; } = [];
    public bool CanEdit { get; private set; }
    public bool CanUpdate { get; private set; }
    public bool CanPublish { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 10;
    public int Total { get; private set; }
    public int TotalAll { get; private set; }
    public int TotalPublished { get; private set; }
    public int TotalDraft { get; private set; }
    public int TotalPinned { get; private set; }
    public string? Query { get; private set; }
    public string KindFilter { get; private set; } = "TIN_TUC";
    public string KindTitle => KindFilter switch { "HUONG_DAN_SO" => "Bình dân học vụ số", "CHUONG_TRINH_HOC" => "Chương trình học", _ => "Tin tức - sự kiện" };
    public string? Status { get; private set; }
    public Guid? CategoryId { get; private set; }
    public bool PinnedOnly { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public string? ErrorMessage { get; private set; }
    public string CurrentPathAndQuery { get; private set; } = "/admin/cms/bai-viet";

    public async Task<IActionResult> OnGetAsync(string? q, string? status, Guid? categoryId, string? pinned, string? sort, string? dir, string? kind, int page = 1, int pageSize = 10)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        CanUpdate = Has("cms.update");
        CanPublish = Has("cms.publish");
        KindFilter = kind is "HUONG_DAN_SO" or "CHUONG_TRINH_HOC" ? kind : "TIN_TUC";
        ErrorMessage = TempData["CmsBaiVietError"] as string;
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        Status = NormalizeStatus(status);
        CategoryId = categoryId;
        PinnedOnly = pinned is "1" or "true" or "on";
        Sort = NormalizeSort(sort);
        Dir = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        if (int.TryParse(Request.Query["page"], out var queryPage) && queryPage > 0) page = queryPage;
        if (int.TryParse(Request.Query["pageSize"], out var querySize) && querySize > 0) pageSize = querySize;
        PageSize = PageSizes.Contains(pageSize) ? pageSize : 10;
        CurrentPage = Math.Max(1, page);
        CurrentPathAndQuery = Request.Path + Request.QueryString;

        var catsTask = _api.GetJsonAsync<ListEnvelope<CategoryOption>>("/api/v1/admin/categories?kind=" + Uri.EscapeDataString(KindFilter), token);
        var allTask = CountAsync(token, null, null);
        var publishedTask = CountAsync(token, "PUBLISHED", null);
        var draftTask = CountAsync(token, "DRAFT", null);
        var pinnedTask = CountPinnedAsync(token);
        var listTask = LoadItemsAsync(token);

        await Task.WhenAll(catsTask, allTask, publishedTask, draftTask, pinnedTask, listTask);

        Categories = catsTask.Result?.Items ?? [];
        TotalAll = allTask.Result;
        TotalPublished = publishedTask.Result;
        TotalDraft = draftTask.Result;
        TotalPinned = pinnedTask.Result;
        return Page();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id, string? returnUrl)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/posts/" + id + "/publish", token, new { });
        if (response is null || !response.IsSuccessStatusCode) TempData["CmsBaiVietError"] = "Không đăng được bài viết.";
        return BackToList(returnUrl);
    }

    public async Task<IActionResult> OnPostUnpublishAsync(Guid id, string? returnUrl)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/posts/" + id + "/unpublish", token, new { });
        if (response is null || !response.IsSuccessStatusCode) TempData["CmsBaiVietError"] = "Không gỡ đăng được bài viết.";
        return BackToList(returnUrl);
    }

    public async Task<IActionResult> OnPostPinAsync(Guid id, bool pinned, string? returnUrl)
    {
        if (!Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (!await SetPinnedAsync(token, id, pinned)) TempData["CmsBaiVietError"] = pinned ? "Không ghim được bài viết." : "Không bỏ ghim được bài viết.";
        return BackToList(returnUrl);
    }

    public async Task<IActionResult> OnPostBulkAsync(string? action, Guid[]? ids, string? returnUrl)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var selected = ids?.Where(id => id != Guid.Empty).Distinct().ToArray() ?? [];
        if (selected.Length == 0)
        {
            TempData["CmsBaiVietError"] = "Chọn ít nhất một bài viết.";
            return BackToList(returnUrl);
        }

        if (action is "publish" or "unpublish" && !Has("cms.publish")) return Redirect("/admin/khong-quyen");
        if (action is "pin" or "unpin" && !Has("cms.update")) return Redirect("/admin/khong-quyen");

        var failed = 0;
        foreach (var id in selected)
        {
            var ok = action switch
            {
                "publish" => await SendOk(token, HttpMethod.Post, "/api/v1/admin/posts/" + id + "/publish"),
                "unpublish" => await SendOk(token, HttpMethod.Post, "/api/v1/admin/posts/" + id + "/unpublish"),
                "pin" => await SetPinnedAsync(token, id, true),
                "unpin" => await SetPinnedAsync(token, id, false),
                _ => false
            };
            if (!ok) failed++;
        }

        if (action is not "publish" and not "unpublish" and not "pin" and not "unpin")
            TempData["CmsBaiVietError"] = "Chọn một thao tác hàng loạt.";
        else if (failed > 0) TempData["CmsBaiVietError"] = "Một số bài viết chưa xử lý được. Thử lại từng bài.";
        return BackToList(returnUrl);
    }

    public string ListUrl(int? page = null, int? pageSize = null, string? status = null, Guid? categoryId = null, bool? pinned = null, string? q = null, string? sort = null, string? dir = null, bool reset = false)
    {
        var query = new Dictionary<string, string?>();
        var nextQ = reset ? null : q ?? Query;
        var nextStatus = reset ? null : status ?? Status;
        var nextCategory = reset ? null : categoryId ?? CategoryId;
        var nextPinned = reset ? false : pinned ?? PinnedOnly;
        var nextSort = reset ? null : sort ?? Sort;
        var nextDir = reset ? "asc" : dir ?? Dir;
        var nextSize = pageSize ?? PageSize;
        var nextPage = page ?? CurrentPage;
        query["kind"] = KindFilter;
        if (!string.IsNullOrWhiteSpace(nextQ)) query["q"] = nextQ;
        if (!string.IsNullOrWhiteSpace(nextStatus)) query["status"] = nextStatus;
        if (nextCategory is Guid cat) query["categoryId"] = cat.ToString();
        if (nextPinned) query["pinned"] = "1";
        if (!string.IsNullOrWhiteSpace(nextSort)) query["sort"] = nextSort;
        if (!string.IsNullOrWhiteSpace(nextSort) && nextDir == "desc") query["dir"] = "desc";
        if (nextSize != 10) query["pageSize"] = nextSize.ToString(CultureInfo.InvariantCulture);
        if (nextPage > 1) query["page"] = nextPage.ToString(CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString("/admin/cms/bai-viet", query);
    }

    public string SortUrl(string column)
    {
        var nextDir = string.Equals(Sort, column, StringComparison.OrdinalIgnoreCase) && Dir == "asc" ? "desc" : "asc";
        return ListUrl(page: 1, sort: column, dir: nextDir);
    }

    public string StatusLabel(string? status) => status switch
    {
        "PUBLISHED" => "Đã đăng",
        "DRAFT" => "Bản nháp",
        "ARCHIVED" => "Lưu trữ",
        _ => EducationUi.Status(status ?? "")
    };

    public string FormatStamp(DateTimeOffset? value)
    {
        if (value is null) return "—";
        return value.Value.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
    }

    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(PageSize, 1)));
    public int FromItem => Total == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int ToItem => Math.Min(CurrentPage * PageSize, Total);
    public bool FiltersActive => Query is not null || Status is not null || CategoryId is not null || PinnedOnly;

    private async Task LoadItemsAsync(string token)
    {
        if (PinnedOnly)
        {
            var batch = await FetchAsync(token, 1, 100, Status, CategoryId, Query);
            var pinned = SortItems((batch.Items ?? []).Where(item => item.Pinned));
            Total = pinned.Count;
            CurrentPage = Math.Min(CurrentPage, Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)));
            Items = pinned.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToArray();
            return;
        }

        var list = await FetchAsync(token, CurrentPage, PageSize, Status, CategoryId, Query);
        Items = SortItems(list.Items ?? []);
        CurrentPage = list.Page < 1 ? CurrentPage : list.Page;
        PageSize = PageSizes.Contains(list.PageSize) ? list.PageSize : PageSize;
        Total = list.Total;
        if (Sort is not null) Items = SortItems(Items);
    }

    private async Task<int> CountAsync(string token, string? status, Guid? categoryId)
    {
        var result = await FetchAsync(token, 1, 1, status, categoryId, null);
        return result.Total;
    }

    private async Task<int> CountPinnedAsync(string token)
    {
        var result = await FetchAsync(token, 1, 100, null, null, null);
        return (result.Items ?? []).Count(item => item.Pinned);
    }

    private async Task<ListEnvelope<PostItem>> FetchAsync(string token, int page, int pageSize, string? status, Guid? categoryId, string? q)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
            ["kind"] = KindFilter
        };
        if (!string.IsNullOrWhiteSpace(status)) query["status"] = status;
        if (categoryId is Guid id) query["categoryId"] = id.ToString();
        if (!string.IsNullOrWhiteSpace(q)) query["q"] = q;
        var path = QueryHelpers.AddQueryString("/api/v1/admin/posts", query);
        return await _api.GetJsonAsync<ListEnvelope<PostItem>>(path, token)
            ?? new ListEnvelope<PostItem>([], page, pageSize, 0);
    }

    private IReadOnlyList<PostItem> SortItems(IEnumerable<PostItem> source)
    {
        var items = source as IList<PostItem> ?? source.ToList();
        if (string.IsNullOrWhiteSpace(Sort)) return items as IReadOnlyList<PostItem> ?? items.ToArray();
        IEnumerable<PostItem> ordered = Sort switch
        {
            "title" => items.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase),
            "category" => items.OrderBy(item => item.CategoryName, StringComparer.CurrentCultureIgnoreCase),
            "status" => items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
            "pinned" => items.OrderBy(item => item.Pinned),
            "updated" => items.OrderBy(item => item.PublishedAt ?? DateTimeOffset.MinValue),
            _ => items.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase)
        };
        if (Dir == "desc") ordered = ordered.Reverse();
        return ordered.ToArray();
    }

    private async Task<bool> SetPinnedAsync(string token, Guid id, bool pinned)
    {
        var body = await _api.GetJsonAsync<ItemEnvelope<PostDetail>>("/api/v1/admin/posts/" + id, token);
        if (body?.Item is not { } item) return false;
        var payload = new
        {
            categoryId = item.CategoryId,
            title = item.Title,
            slug = item.Slug,
            summary = item.Summary,
            html = item.Html,
            coverKey = item.CoverKey,
            thumbnailKey = item.ThumbnailKey,
            pinned,
            seoTitle = item.SeoTitle,
            seoDescription = item.SeoDescription
        };
        return await SendOk(token, HttpMethod.Put, "/api/v1/admin/posts/" + id, payload);
    }

    private async Task<bool> SendOk(string token, HttpMethod method, string path, object? body = null)
    {
        var response = await _api.SendJsonAsync(method, path, token, body ?? new { });
        return response is not null && response.IsSuccessStatusCode;
    }

    private IActionResult BackToList(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl)
            && returnUrl.StartsWith("/admin/cms/bai-viet", StringComparison.Ordinal)
            && !returnUrl.Contains('\n', StringComparison.Ordinal)
            && !returnUrl.Contains('\r', StringComparison.Ordinal))
        {
            return Redirect(returnUrl);
        }

        return Redirect("/admin/cms/bai-viet");
    }

    private static string? NormalizeStatus(string? status)
    {
        var value = status?.Trim().ToUpperInvariant();
        return value is "PUBLISHED" or "DRAFT" or "ARCHIVED" ? value : null;
    }

    private static string? NormalizeSort(string? sort)
    {
        var value = sort?.Trim().ToLowerInvariant();
        return value is "title" or "category" or "status" or "pinned" or "updated" ? value : null;
    }

    private bool HasView() => Has("cms.view") || Has("cms.create") || Has("cms.update") || Has("cms.publish") || Has("cms.delete");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record PostItem(Guid Id, Guid CategoryId, string Title, string Slug, string Status, bool Pinned, string CategoryName, DateTimeOffset? PublishedAt, string? Summary);
    public sealed record CategoryOption(Guid Id, string Name);
    public sealed record PostDetail(Guid Id, Guid CategoryId, string Title, string Slug, string? Summary, string Html, string? CoverKey, string Status, bool Pinned, string? SeoTitle, string? SeoDescription, DateTimeOffset? PublishedAt, string? ThumbnailKey = null);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int Page = 1, int PageSize = 10, int Total = 0);
}
