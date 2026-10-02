using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class DoiTuongModel : PageModel
{
    private readonly BackendApiClient _api;
    public DoiTuongModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<Item> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    public bool Editing => Id.HasValue;
    public bool ShowModal { get; private set; }
    public string? Query { get; private set; }
    public string? StatusFilter { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public int TotalAll { get; private set; }
    public int TotalActive { get; private set; }
    public int TotalInactive { get; private set; }
    public bool FiltersActive => Query is not null || StatusFilter is not null;

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public bool Active { get; set; } = true;

    public async Task<IActionResult> OnGetAsync(Guid? id, bool create = false, string? q = null, string? status = null, string? sort = null, string? dir = null)
    {
        ShowModal = create || id.HasValue;
        return await LoadAsync(id, q: q, status: status, sort: sort, dir: dir);
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
        return parts.Count == 0 ? "/admin/doi-tuong" : "/admin/doi-tuong?" + string.Join("&", parts);
    }

    public string SortUrl(string column) => ListUrl(sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { code = Code, name = Name, active = Active };
        var response = Id is Guid id
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/audiences/{id}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/audiences", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được đối tượng. Kiểm tra mã trùng.";
            ShowModal = true;
            return await LoadAsync(Id, hydrateDetail: false);
        }

        return Redirect("/admin/doi-tuong");
    }

    private bool HasManage() => Has("education.manage");
    private bool HasView() => Has("education.manage") || Has("education.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(Guid? id, bool hydrateDetail = true, string? q = null, string? status = null, string? sort = null, string? dir = null)
    {
        CanManage = HasManage();
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        StatusFilter = status is "active" or "inactive" ? status : null;
        Sort = CmsListSort.Normalize(sort, "code", "name", "status");
        Dir = CmsListSort.Dir(dir);
        var body = await _api.GetJsonAsync<ListEnvelope<Item>>("/api/v1/admin/audiences?pageSize=100", token);
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
        if (hydrateDetail && id is Guid editId)
        {
            var detail = await _api.GetJsonAsync<ItemEnvelope<Item>>($"/api/v1/admin/audiences/{editId}", token);
            if (detail?.Item is Item item)
            {
                Id = item.Id;
                Code = item.Code;
                Name = item.Name;
                Active = item.Active;
            }
        }

        return Page();
    }

    public sealed record Item(Guid Id, string Code, string Name, bool Active);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ItemEnvelope<T>(T? Item);
}
