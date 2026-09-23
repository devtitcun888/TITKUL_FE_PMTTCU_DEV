using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CauHinhModel : PageModel
{
    private readonly BackendApiClient _api;
    public CauHinhModel(BackendApiClient api) => _api = api;
    public Dictionary<string, string> Values { get; private set; } = [];
    public string? Message { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool CanEdit { get; private set; }
    [BindProperty] public string Key { get; set; } = "";
    [BindProperty] public string Value { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Has("cms.create") && !Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/site-config", token, new { key = Key, value = Value });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không lưu được cấu hình.";
        else Message = "Đã cập nhật. Cổng thông tin dùng giá trị mới.";
        return await LoadAsync();
    }

    private async Task<IActionResult> LoadAsync()
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create") || Has("cms.update");
        var body = await _api.GetJsonAsync<ConfigEnvelope>("/api/v1/admin/site-config", token);
        Values = body?.Item ?? [];
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];
    public sealed record ConfigEnvelope(Dictionary<string, string>? Item);
}
