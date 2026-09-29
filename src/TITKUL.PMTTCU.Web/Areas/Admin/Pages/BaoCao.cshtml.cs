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
    public bool CanExport { get; private set; }
    public int CurrentYear => VietnamToday().Year;
    [BindProperty(SupportsGet = true)] public string? From { get; set; }
    [BindProperty(SupportsGet = true)] public string? To { get; set; }
    [BindProperty(SupportsGet = true)] public string? Period { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Has("report.system.view") && !Has("report.own_classes.view")) return Redirect("/admin/khong-quyen");
        CanExport = Has("report.export");
        ApplyPeriod();
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

    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (!Has("report.export")) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");
        ApplyPeriod();
        if (DateOnly.TryParse(From, out var start) && DateOnly.TryParse(To, out var end) && end < start) return BadRequest();
        var file = await _api.GetFileAsync("/api/v1/admin/reports/pdf" + Query(), token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, "application/pdf", "bao-cao-tong-hop.pdf");
    }

    private void ApplyPeriod()
    {
        if (Period is null) return;
        var today = VietnamToday();
        if (Period.StartsWith("Q:", StringComparison.Ordinal))
        {
            var parts = Period[2..].Split("-Q", StringSplitOptions.None);
            if (parts.Length == 2
                && int.TryParse(parts[0], out var quarterYear)
                && int.TryParse(parts[1], out var quarter)
                && quarterYear is >= 2000 and <= 2100
                && quarter is >= 1 and <= 4)
            {
                var first = new DateOnly(quarterYear, (quarter - 1) * 3 + 1, 1);
                From = first.ToString("yyyy-MM-dd");
                To = first.AddMonths(3).AddDays(-1).ToString("yyyy-MM-dd");
            }
        }
        else if (Period.StartsWith("Y:", StringComparison.Ordinal)
            && int.TryParse(Period.AsSpan(2), out var year)
            && year is >= 2000 and <= 2100)
        {
            From = new DateOnly(year, 1, 1).ToString("yyyy-MM-dd");
            To = new DateOnly(year, 12, 31).ToString("yyyy-MM-dd");
        }
        else if (Period == "current-quarter")
        {
            var quarter = ((today.Month - 1) / 3) + 1;
            var first = new DateOnly(today.Year, (quarter - 1) * 3 + 1, 1);
            From = first.ToString("yyyy-MM-dd");
            To = first.AddMonths(3).AddDays(-1).ToString("yyyy-MM-dd");
        }
        else if (Period == "current-year")
        {
            From = new DateOnly(today.Year, 1, 1).ToString("yyyy-MM-dd");
            To = new DateOnly(today.Year, 12, 31).ToString("yyyy-MM-dd");
        }
    }

    private string Query()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(From)) parts.Add("from=" + Uri.EscapeDataString(From));
        if (!string.IsNullOrWhiteSpace(To)) parts.Add("to=" + Uri.EscapeDataString(To));
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private static DateOnly VietnamToday() =>
        DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);

    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    public sealed record AttendanceRow(string ClassName, int Occurred, int Roster, int Present, decimal Percent);
    public sealed record SummaryBody(int Classes, int Learners, decimal AttendancePercent, IReadOnlyList<AttendanceRow> Attendance);
    private sealed record Envelope(SummaryBody? Item);
}
