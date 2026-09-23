using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsThongBaoModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsThongBaoModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<NoticeItem> Items { get; private set; } = [];
    public bool CanEdit { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        var list = await _api.GetJsonAsync<ListEnvelope<NoticeItem>>("/api/v1/admin/notices?pageSize=50", token);
        Items = list?.Items ?? [];
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record NoticeItem(Guid Id, string Title, string Slug, string Level, string Status);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
