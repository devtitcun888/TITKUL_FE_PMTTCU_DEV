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
    public bool CanEdit { get; private set; }

    [BindProperty] public string Kind { get; set; } = "TIN_TUC";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Slug { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Has("cms.create")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/categories", token, new { kind = Kind, name = Name, slug = Slug, active = true, sort = 0 });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không tạo được chuyên mục. Kiểm tra tên và đường dẫn.";
            return await LoadAsync();
        }

        return Redirect("/admin/cms/chuyen-muc");
    }

    private async Task<IActionResult> LoadAsync()
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        var list = await _api.GetJsonAsync<ListEnvelope<CategoryItem>>("/api/v1/admin/categories", token);
        Items = list?.Items ?? [];
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record CategoryItem(Guid Id, string Kind, string Name, string Slug, bool Active);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
