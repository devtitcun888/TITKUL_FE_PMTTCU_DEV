using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class LienHeAdminModel : PageModel
{
    private static readonly int[] PageSizes = [10, 20, 50];
    private readonly BackendApiClient _api;
    public LienHeAdminModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<ContactItem> Items { get; private set; } = [];
    public bool CanEdit { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public int TotalAll { get; private set; }
    public int TotalNew { get; private set; }
    public int TotalProcessing { get; private set; }
    public int TotalClosed { get; private set; }
    public string? Status { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public string CurrentPathAndQuery { get; private set; } = "/admin/lien-he";
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(PageSize, 1)));
    public int FromItem => Total == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int ToItem => Math.Min(CurrentPage * PageSize, Total);
    public bool FiltersActive => Status is not null;

    public async Task<IActionResult> OnGetAsync(string? status, string? sort, string? dir, int page = 1, int pageSize = 20)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(page, status, pageSize, sort, dir);
    }

    public async Task<IActionResult> OnPostStatusAsync(Guid id, string status, string? returnUrl)
    {
        if (!Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/contacts/" + id, token, new { status });
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/admin/lien-he" : returnUrl);
    }

    public string ListUrl(int? page = null, int? pageSize = null, string? status = null, string? sort = null, string? dir = null)
    {
        var query = new Dictionary<string, string?>();
        var nextStatus = status ?? Status;
        if (status == "") nextStatus = null;
        var nextSize = pageSize ?? PageSize;
        var nextPage = page ?? CurrentPage;
        if (!string.IsNullOrWhiteSpace(nextStatus)) query["status"] = nextStatus;
        CmsListSort.Append(query, sort ?? Sort, dir ?? Dir);
        if (nextSize != 20) query["pageSize"] = nextSize.ToString(CultureInfo.InvariantCulture);
        if (nextPage > 1) query["page"] = nextPage.ToString(CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString("/admin/lien-he", query);
    }

    public string SortUrl(string column) => ListUrl(page: 1, sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    private async Task<IActionResult> LoadAsync(int page = 1, string? status = null, int pageSize = 20, string? sort = null, string? dir = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.update");
        Status = status is "NEW" or "PROCESSING" or "CLOSED" or "SPAM" ? status : null;
        Sort = CmsListSort.Normalize(sort, "name", "title", "status");
        Dir = CmsListSort.Dir(dir);
        PageSize = PageSizes.Contains(pageSize) ? pageSize : 20;
        CurrentPage = Math.Max(1, page);
        CurrentPathAndQuery = Request.Path + Request.QueryString;

        var allTask = CountAsync(token, null);
        var newTask = CountAsync(token, "NEW");
        var processingTask = CountAsync(token, "PROCESSING");
        var closedTask = CountAsync(token, "CLOSED");
        var listTask = FetchAsync(token, CurrentPage, PageSize, Status);
        await Task.WhenAll(allTask, newTask, processingTask, closedTask, listTask);

        TotalAll = allTask.Result;
        TotalNew = newTask.Result;
        TotalProcessing = processingTask.Result;
        TotalClosed = closedTask.Result;
        var list = listTask.Result;
        Items = list.Items ?? [];
        CurrentPage = list.Page ?? CurrentPage;
        if (list.PageSize is int size && PageSizes.Contains(size)) PageSize = size;
        Total = list.Total ?? Items.Count;
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "title" => Items.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase),
                "status" => Items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                _ => Items.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        return Page();
    }

    private async Task<int> CountAsync(string token, string? status)
    {
        var result = await FetchAsync(token, 1, 1, status);
        return result.Total ?? 0;
    }

    private async Task<ListEnvelope<ContactItem>> FetchAsync(string token, int page, int pageSize, string? status)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(status)) query["status"] = status;
        var path = QueryHelpers.AddQueryString("/api/v1/admin/contacts", query);
        return await _api.GetJsonAsync<ListEnvelope<ContactItem>>(path, token)
            ?? new ListEnvelope<ContactItem>([], page, pageSize, 0);
    }

    private bool HasView() => Has("cms.view") || Has("cms.update");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record ContactItem(Guid Id, string Name, string Phone, string Title, string Body, string Status);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
