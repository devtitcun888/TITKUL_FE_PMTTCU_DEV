using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsChuyenMucModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsChuyenMucModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<CategoryItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanCreate { get; private set; }
    public bool CanUpdate { get; private set; }
    public bool CanDelete { get; private set; }
    public CategoryItem? EditingCategory { get; private set; }
    public string? Query { get; private set; }
    public bool? ActiveFilter { get; private set; }
    public string? SortBy { get; private set; }
    public string Dir { get; private set; } = "asc";
    public int TotalAll { get; private set; }
    public int TotalActive { get; private set; }
    public int TotalHidden { get; private set; }
    public bool FiltersActive => Query is not null || ActiveFilter is not null;

    [BindProperty] public string Kind { get; set; } = "TIN_TUC";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Slug { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public int Sort { get; set; }
    [BindProperty] public bool Active { get; set; } = true;

    public async Task<IActionResult> OnGetAsync(Guid? edit, string? q, string? active, string? sort, string? dir)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(edit, q, active, sort, dir);
    }

    public string ListUrl(string? q = null, string? active = null, string? sort = null, string? dir = null)
    {
        var parts = new List<string>();
        var nextQ = q ?? Query;
        var nextActive = active ?? (ActiveFilter is true ? "1" : ActiveFilter is false ? "0" : null);
        if (active == "") nextActive = null;
        if (!string.IsNullOrWhiteSpace(nextQ)) parts.Add("q=" + Uri.EscapeDataString(nextQ));
        if (!string.IsNullOrWhiteSpace(nextActive)) parts.Add("active=" + Uri.EscapeDataString(nextActive));
        CmsListSort.AppendParts(parts, sort ?? SortBy, dir ?? Dir);
        return parts.Count == 0 ? "/admin/cms/chuyen-muc" : "/admin/cms/chuyen-muc?" + string.Join("&", parts);
    }

    public string SortUrl(string column) => ListUrl(sort: column, dir: CmsListSort.NextDir(SortBy, column, Dir));

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!Has("cms.create")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/categories", token, new { kind = Kind, name = Name, slug = Slug, description = Description, active = Active, sort = Sort });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không tạo được chuyên mục. Kiểm tra tên và đường dẫn.";
            return await LoadAsync();
        }

        return Redirect("/admin/cms/chuyen-muc");
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id)
    {
        if (!Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/categories/{id}", token, new { kind = Kind, name = Name, slug = Slug, description = Description, active = Active, sort = Sort });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không cập nhật được chuyên mục. Kiểm tra tên, đường dẫn và quyền chỉnh sửa.";
            await LoadAsync();
            EditingCategory = Items.FirstOrDefault(item => item.Id == id);
            return Page();
        }

        return Redirect("/admin/cms/chuyen-muc");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!Has("cms.delete")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/categories/{id}", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = response?.StatusCode == System.Net.HttpStatusCode.Conflict
                ? "Không thể xóa chuyên mục đang được bài viết sử dụng. Hãy chuyển các bài sang chuyên mục khác trước."
                : "Không xóa được chuyên mục.";
            return await LoadAsync();
        }

        return Redirect("/admin/cms/chuyen-muc");
    }

    private async Task<IActionResult> LoadAsync(Guid? edit = null, string? q = null, string? active = null, string? sort = null, string? dir = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanCreate = Has("cms.create");
        CanUpdate = Has("cms.update");
        CanDelete = Has("cms.delete");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        ActiveFilter = active is "1" or "true" ? true : active is "0" or "false" ? false : null;
        SortBy = CmsListSort.Normalize(sort, "name", "slug", "kind", "order", "status");
        Dir = CmsListSort.Dir(dir);
        var list = await _api.GetJsonAsync<ListEnvelope<CategoryItem>>("/api/v1/admin/categories", token);
        var all = list?.Items ?? [];
        TotalAll = all.Count;
        TotalActive = all.Count(item => item.Active);
        TotalHidden = all.Count(item => !item.Active);
        IEnumerable<CategoryItem> filtered = all;
        if (ActiveFilter is bool flag) filtered = filtered.Where(item => item.Active == flag);
        if (Query is not null)
        {
            filtered = filtered.Where(item =>
                item.Name.Contains(Query, StringComparison.CurrentCultureIgnoreCase)
                || item.Slug.Contains(Query, StringComparison.CurrentCultureIgnoreCase));
        }
        Items = filtered.ToArray();
        if (SortBy is not null)
        {
            Items = CmsListSort.Order(Items, Dir, SortBy switch
            {
                "slug" => Items.OrderBy(item => item.Slug, StringComparer.CurrentCultureIgnoreCase),
                "kind" => Items.OrderBy(item => item.Kind, StringComparer.OrdinalIgnoreCase),
                "order" => Items.OrderBy(item => item.Sort),
                "status" => Items.OrderBy(item => item.Active),
                _ => Items.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        if (edit is Guid id)
        {
            EditingCategory = all.FirstOrDefault(item => item.Id == id);
            if (EditingCategory is null)
            {
                ErrorMessage ??= "Không tìm thấy chuyên mục cần sửa.";
            }
            else
            {
                Kind = EditingCategory.Kind;
                Name = EditingCategory.Name;
                Slug = EditingCategory.Slug;
                Description = EditingCategory.Description;
                Sort = EditingCategory.Sort;
                Active = EditingCategory.Active;
            }
        }
        return Page();
    }

    private bool HasView() => HasCmsAccess();
    private bool HasCmsAccess() => Has("cms.view") || Has("cms.create") || Has("cms.update") || Has("cms.publish") || Has("cms.delete");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record CategoryItem(Guid Id, string Kind, string Name, string Slug, string? Description, int Sort, bool Active);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
