using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsThongBaoModel : PageModel
{
    private static readonly int[] PageSizes = [10, 20, 50];
    private readonly BackendApiClient _api;
    public CmsThongBaoModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<NoticeItem> Items { get; private set; } = [];
    public bool CanEdit { get; private set; }
    public bool CanPublish { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public int TotalAll { get; private set; }
    public int TotalPublished { get; private set; }
    public int TotalDraft { get; private set; }
    public int TotalArchived { get; private set; }
    public string? Query { get; private set; }
    public string? Status { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public string? ErrorMessage { get; private set; }
    public string CurrentPathAndQuery { get; private set; } = "/admin/cms/thong-bao";
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(PageSize, 1)));
    public int FromItem => Total == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int ToItem => Math.Min(CurrentPage * PageSize, Total);
    public bool FiltersActive => Query is not null || Status is not null;

    public async Task<IActionResult> OnGetAsync(string? q, string? status, string? sort, string? dir, int page = 1, int pageSize = 20)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        CanPublish = Has("cms.publish");
        ErrorMessage = TempData["CmsThongBaoError"] as string;
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        Status = NormalizeStatus(status);
        Sort = CmsListSort.Normalize(sort, "title", "level", "status");
        Dir = CmsListSort.Dir(dir);
        PageSize = PageSizes.Contains(pageSize) ? pageSize : 20;
        CurrentPage = Math.Max(1, page);
        CurrentPathAndQuery = Request.Path + Request.QueryString;

        var allTask = CountAsync(token, null);
        var publishedTask = CountAsync(token, "PUBLISHED");
        var draftTask = CountAsync(token, "DRAFT");
        var archivedTask = CountAsync(token, "ARCHIVED");
        var listTask = FetchAsync(token, CurrentPage, PageSize, Status, Query);
        await Task.WhenAll(allTask, publishedTask, draftTask, archivedTask, listTask);

        TotalAll = allTask.Result;
        TotalPublished = publishedTask.Result;
        TotalDraft = draftTask.Result;
        TotalArchived = archivedTask.Result;
        var list = listTask.Result;
        Items = list.Items ?? [];
        CurrentPage = list.Page < 1 ? CurrentPage : list.Page;
        if (PageSizes.Contains(list.PageSize)) PageSize = list.PageSize;
        Total = list.Total;
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "level" => Items.OrderBy(item => item.Level, StringComparer.OrdinalIgnoreCase),
                "status" => Items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                _ => Items.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        return Page();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id, string? returnUrl)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/notices/" + id + "/publish", token, new { });
        if (response is null || !response.IsSuccessStatusCode) TempData["CmsThongBaoError"] = "Không đăng được thông báo.";
        return Back(returnUrl);
    }

    public async Task<IActionResult> OnPostArchiveAsync(Guid id, string? returnUrl)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/notices/" + id + "/archive", token, new { });
        if (response is null || !response.IsSuccessStatusCode) TempData["CmsThongBaoError"] = "Không lưu trữ được thông báo.";
        return Back(returnUrl);
    }

    public string ListUrl(int? page = null, int? pageSize = null, string? status = null, string? q = null, string? sort = null, string? dir = null)
    {
        var query = new Dictionary<string, string?>();
        var nextQ = q ?? Query;
        var nextStatus = status ?? Status;
        var nextSize = pageSize ?? PageSize;
        var nextPage = page ?? CurrentPage;
        if (!string.IsNullOrWhiteSpace(nextQ)) query["q"] = nextQ;
        if (!string.IsNullOrWhiteSpace(nextStatus)) query["status"] = nextStatus;
        CmsListSort.Append(query, sort ?? Sort, dir ?? Dir);
        if (nextSize != 20) query["pageSize"] = nextSize.ToString(CultureInfo.InvariantCulture);
        if (nextPage > 1) query["page"] = nextPage.ToString(CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString("/admin/cms/thong-bao", query);
    }

    public string SortUrl(string column) => ListUrl(page: 1, sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public string StatusLabel(string? status) => status switch
    {
        "PUBLISHED" => "Đã đăng",
        "DRAFT" => "Bản nháp",
        "ARCHIVED" => "Lưu trữ",
        _ => EducationUi.Status(status ?? "")
    };

    private async Task<int> CountAsync(string token, string? status)
    {
        var result = await FetchAsync(token, 1, 1, status, null);
        return result.Total;
    }

    private async Task<ListEnvelope<NoticeItem>> FetchAsync(string token, int page, int pageSize, string? status, string? q)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(status)) query["status"] = status;
        if (!string.IsNullOrWhiteSpace(q)) query["q"] = q;
        var path = QueryHelpers.AddQueryString("/api/v1/admin/notices", query);
        return await _api.GetJsonAsync<ListEnvelope<NoticeItem>>(path, token)
            ?? new ListEnvelope<NoticeItem>([], page, pageSize, 0);
    }

    private IActionResult Back(string? returnUrl) =>
        LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/admin/cms/thong-bao" : returnUrl);
    private static string? NormalizeStatus(string? status) =>
        status is "PUBLISHED" or "DRAFT" or "ARCHIVED" ? status : null;
    private bool HasView() => Has("cms.view") || Has("cms.create") || Has("cms.update") || Has("cms.publish") || Has("cms.delete");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record NoticeItem(Guid Id, string Title, string Slug, string Level, string Status);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int Page, int PageSize, int Total);
}
