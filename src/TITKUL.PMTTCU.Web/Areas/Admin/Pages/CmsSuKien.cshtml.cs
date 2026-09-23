using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsSuKienModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsSuKienModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<EventItem> Items { get; private set; } = [];
    public bool CanEdit { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        var list = await _api.GetJsonAsync<ListEnvelope<EventItem>>("/api/v1/admin/events?pageSize=50", token);
        Items = list?.Items ?? [];
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record EventItem(Guid Id, string Title, string Slug, string Status, DateTimeOffset StartAt, DateTimeOffset EndAt);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
