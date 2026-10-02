using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class ThonApModel : PageModel
{
    private readonly BackendApiClient _api;
    public ThonApModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<Item> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    public bool ShowModal { get; private set; }
    public string? Query { get; private set; }
    public string? StatusFilter { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public int TotalAll { get; private set; }
    public int TotalActive { get; private set; }
    public int TotalInactive { get; private set; }
    public bool FiltersActive => Query is not null || StatusFilter is not null;

    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(bool create = false, string? q = null, string? status = null, string? sort = null, string? dir = null)
    {
        ShowModal = create;
        return await LoadAsync(q, status, sort, dir);
    }

    public string ListUrl(string? q = null, string? status = null, string? sort = null, string? dir = null)
    {
        var parts = new List<string>();
        var nextQ = q ?? Query;
        var nextStatus = status ?? StatusFilter;
        if (status == "") nextStatus = null;
        if (!string.IsNullOrWhiteSpace(nextQ)) parts.Add("q=" + Uri.EscapeDataString(nextQ));
        if (!string.IsNullOrWhiteSpace(nextStatus)) parts.Add("status=" + Uri.EscapeDataString(nextStatus));
        CmsListSort.AppendParts(parts, sort ?? Sort, dir ?? Dir);
        return parts.Count == 0 ? "/admin/thon-ap" : "/admin/thon-ap?" + string.Join("&", parts);
    }

    public string SortUrl(string column) => ListUrl(sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (string.IsNullOrWhiteSpace(token)) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/hamlets", token, new { code = Code, name = Name, order = 0, active = true });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được thôn/ấp. Kiểm tra mã trùng.";
            ShowModal = true;
            return await LoadAsync();
        }

        return Redirect("/admin/thon-ap");
    }

    private bool HasManage() => Has("education.manage");
    private bool HasView() => Has("education.manage") || Has("education.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    private async Task<IActionResult> LoadAsync(string? q = null, string? status = null, string? sort = null, string? dir = null)
    {
        CanManage = HasManage();
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (string.IsNullOrWhiteSpace(token)) return Redirect("/admin/dang-nhap");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        StatusFilter = status is "active" or "inactive" ? status : null;
        Sort = CmsListSort.Normalize(sort, "code", "name", "status");
        Dir = CmsListSort.Dir(dir);
        var body = await _api.GetJsonAsync<ListEnvelope<Item>>("/api/v1/admin/hamlets", token);
        if (body is null) ErrorMessage ??= "Không tải được danh sách.";
        var all = body?.Items ?? [];
        TotalAll = all.Count;
        TotalActive = all.Count(item => item.Active);
        TotalInactive = all.Count(item => !item.Active);
        IEnumerable<Item> filtered = all;
        if (StatusFilter == "active") filtered = filtered.Where(item => item.Active);
        else if (StatusFilter == "inactive") filtered = filtered.Where(item => !item.Active);
        if (Query is not null)
        {
            filtered = filtered.Where(item =>
                item.Code.Contains(Query, StringComparison.CurrentCultureIgnoreCase)
                || item.Name.Contains(Query, StringComparison.CurrentCultureIgnoreCase));
        }
        Items = filtered.ToArray();
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "code" => Items.OrderBy(item => item.Code, StringComparer.CurrentCultureIgnoreCase),
                "status" => Items.OrderBy(item => item.Active),
                _ => Items.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        return Page();
    }

    public sealed record Item(Guid Id, string Code, string Name, bool Active);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
