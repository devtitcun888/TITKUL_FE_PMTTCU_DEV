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
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? q, int page = 1)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        CurrentPage = Math.Max(1, page);
        var list = await _api.GetJsonAsync<ListEnvelope<EventItem>>($"/api/v1/admin/events?page={CurrentPage}&pageSize={PageSize}&q={Uri.EscapeDataString(Query ?? "")}", token);
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? CurrentPage;
        PageSize = list?.PageSize ?? PageSize;
        Total = list?.Total ?? Items.Count;
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record EventItem(Guid Id, string Title, string Slug, string Status, DateTimeOffset StartAt, DateTimeOffset EndAt);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
