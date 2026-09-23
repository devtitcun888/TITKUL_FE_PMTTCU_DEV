using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class LienHeAdminModel : PageModel
{
    private readonly BackendApiClient _api;
    public LienHeAdminModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<ContactItem> Items { get; private set; } = [];
    public bool CanEdit { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostStatusAsync(Guid id, string status)
    {
        if (!Has("cms.update") && !Has("cms.create")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/contacts/" + id, token, new { status });
        return Redirect("/admin/lien-he");
    }

    private async Task<IActionResult> LoadAsync()
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create") || Has("cms.update");
        var list = await _api.GetJsonAsync<ListEnvelope<ContactItem>>("/api/v1/admin/contacts", token);
        Items = list?.Items ?? [];
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record ContactItem(Guid Id, string Name, string Phone, string Title, string Body, string Status);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
