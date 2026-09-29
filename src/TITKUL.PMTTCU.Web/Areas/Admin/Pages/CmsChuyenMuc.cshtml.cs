using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
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
    public CategoryItem? EditingCategory { get; private set; }

    [BindProperty] public string Kind { get; set; } = "TIN_TUC";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Slug { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public int Sort { get; set; }
    [BindProperty] public bool Active { get; set; } = true;

    public async Task<IActionResult> OnGetAsync(Guid? edit)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(edit);
    }

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
        if (!Has("cms.update")) return Redirect("/admin/khong-quyen");
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

    private async Task<IActionResult> LoadAsync(Guid? edit = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanCreate = Has("cms.create");
        CanUpdate = Has("cms.update");
        var list = await _api.GetJsonAsync<ListEnvelope<CategoryItem>>("/api/v1/admin/categories", token);
        Items = list?.Items ?? [];
        if (edit is Guid id)
        {
            EditingCategory = Items.FirstOrDefault(item => item.Id == id);
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

    private bool HasView() => Has("cms.view") || Has("cms.create") || Has("cms.update");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record CategoryItem(Guid Id, string Kind, string Name, string Slug, string? Description, int Sort, bool Active);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
