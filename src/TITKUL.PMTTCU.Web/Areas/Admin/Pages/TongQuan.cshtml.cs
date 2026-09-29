using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class TongQuanModel : PageModel
{
    private readonly BackendApiClient _api;
    public TongQuanModel(BackendApiClient api) => _api = api;
    public SummaryBody? Item { get; private set; }
    public ContentSummary? ContentStats { get; private set; }
    public bool CanExport { get; private set; }
    public string? ErrorMessage { get; private set; }
    [BindProperty(SupportsGet = true)] public string? From { get; set; }
    [BindProperty(SupportsGet = true)] public string? To { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Has("report.system.view") && !Has("report.own_classes.view")) return Redirect("/admin/khong-quyen");
        CanExport = Has("report.export");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");
        if (DateOnly.TryParse(From, out var start) && DateOnly.TryParse(To, out var end) && end < start)
        {
            ErrorMessage = "Đến ngày phải sau hoặc bằng từ ngày.";
            return Page();
        }

        var query = Query();
        var body = await _api.GetJsonAsync<Envelope>("/api/v1/admin/dashboard/summary" + query, token);
        Item = body?.Item;
        if (Item is null) ErrorMessage = "Chưa tải được số liệu.";
        if (Has("cms.view") || Has("cms.create") || Has("cms.update"))
        {
            var counts = await Task.WhenAll(
                CountAsync("/api/v1/admin/posts?page=1&pageSize=1", token),
                CountAsync("/api/v1/admin/notices?page=1&pageSize=1", token),
                CountAsync("/api/v1/admin/events?page=1&pageSize=1", token),
                CountAsync("/api/v1/admin/documents?page=1&pageSize=1", token),
                CountAsync("/api/v1/admin/forms?page=1&pageSize=1", token),
                CountAsync("/api/v1/admin/albums?page=1&pageSize=1", token),
                CountAsync("/api/v1/admin/contacts?status=NEW&page=1&pageSize=1", token));
            ContentStats = new ContentSummary(counts[0], counts[1], counts[2], counts[3], counts[4], counts[5], counts[6]);
        }
        return Page();
    }

    private async Task<int?> CountAsync(string path, string token) =>
        (await _api.GetJsonAsync<CountEnvelope>(path, token))?.Total;

    private string Query()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(From)) parts.Add("from=" + Uri.EscapeDataString(From));
        if (!string.IsNullOrWhiteSpace(To)) parts.Add("to=" + Uri.EscapeDataString(To));
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    public sealed record StatusCount(string Status, int Count);
    public sealed record NamedCount(string Name, int Count);
    public sealed record AgeCount(string Band, int Count);
    public sealed record AttendanceRow(string ClassName, int Occurred, int Roster, int Present, decimal Percent);
    public sealed record SummaryBody(int Classes, int Learners, decimal AttendancePercent, IReadOnlyList<StatusCount> ClassStatus, IReadOnlyList<NamedCount> Hamlets, IReadOnlyList<AgeCount> Ages, IReadOnlyList<NamedCount> Audiences, IReadOnlyList<AttendanceRow> Attendance);
    public sealed record ContentSummary(int? Posts, int? Notices, int? Events, int? Documents, int? Forms, int? Albums, int? NewContacts);
    private sealed record CountEnvelope(int? Total);
    private sealed record Envelope(SummaryBody? Item);
}
