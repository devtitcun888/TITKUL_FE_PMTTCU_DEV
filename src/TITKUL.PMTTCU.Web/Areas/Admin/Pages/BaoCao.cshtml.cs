using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class BaoCaoModel : PageModel
{
    private readonly BackendApiClient _api;
    public BaoCaoModel(BackendApiClient api) => _api = api;
    public SummaryBody? Item { get; private set; }
    public string? ErrorMessage { get; private set; }
    [BindProperty(SupportsGet = true)] public string? From { get; set; }
    [BindProperty(SupportsGet = true)] public string? To { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Has("report.system.view") && !Has("report.own_classes.view")) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");
        if (DateOnly.TryParse(From, out var start) && DateOnly.TryParse(To, out var end) && end < start)
        {
            ErrorMessage = "Đến ngày phải sau hoặc bằng từ ngày.";
            return Page();
        }

        var body = await _api.GetJsonAsync<Envelope>("/api/v1/admin/dashboard/summary" + Query(), token);
        Item = body?.Item;
        if (Item is null) ErrorMessage = "Chưa tải được số liệu.";
        return Page();
    }

    private string Query()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(From)) parts.Add("from=" + Uri.EscapeDataString(From));
        if (!string.IsNullOrWhiteSpace(To)) parts.Add("to=" + Uri.EscapeDataString(To));
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    public sealed record AttendanceRow(string ClassName, int Occurred, int Roster, int Present, decimal Percent);
    public sealed record SummaryBody(int Classes, int Learners, decimal AttendancePercent, IReadOnlyList<AttendanceRow> Attendance);
    private sealed record Envelope(SummaryBody? Item);
}
