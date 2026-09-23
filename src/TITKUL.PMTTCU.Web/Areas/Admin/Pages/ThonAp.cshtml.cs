using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class ThonApModel : PageModel
{
    private readonly BackendApiClient _api;
    public ThonApModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<Item> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";

    public async Task<IActionResult> OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (string.IsNullOrWhiteSpace(token)) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/hamlets", token, new { code = Code, name = Name, order = 0, active = true });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được thôn/ấp. Kiểm tra mã trùng.";
            return await LoadAsync();
        }

        return Redirect("/admin/thon-ap");
    }

    private bool HasManage() => Has("education.manage");
    private bool HasView() => Has("education.manage") || Has("education.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    private async Task<IActionResult> LoadAsync()
    {
        CanManage = HasManage();
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (string.IsNullOrWhiteSpace(token)) return Redirect("/admin/dang-nhap");
        var body = await _api.GetJsonAsync<ListEnvelope<Item>>("/api/v1/admin/hamlets", token);
        if (body is null) ErrorMessage ??= "Không tải được danh sách.";
        Items = body?.Items ?? [];
        return Page();
    }

    public sealed record Item(Guid Id, string Code, string Name, bool Active);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
