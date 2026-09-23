using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class BaoCaoThangModel : PageModel
{
    private readonly BackendApiClient _api;
    public BaoCaoThangModel(BackendApiClient api) => _api = api;
    public SummaryBody? Item { get; private set; }
    public string? ErrorMessage { get; private set; }
    [BindProperty(SupportsGet = true)] public string? Month { get; set; }
    [BindProperty(SupportsGet = true)] public string? From { get; set; }
    [BindProperty(SupportsGet = true)] public string? To { get; set; }

    public async Task<IActionResult> OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnGetXuatAsync()
    {
        if (!Has("report.export")) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");
        ApplyMonth();
        var file = await _api.GetFileAsync("/api/v1/admin/reports/monthly/export" + Query(), token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bao-cao-thang.xlsx");
    }

    private async Task<IActionResult> LoadAsync()
    {
        if (!Has("report.export")) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");
        ApplyMonth();
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

    private void ApplyMonth()
    {
        if (string.IsNullOrWhiteSpace(Month)) return;
        if (!DateOnly.TryParseExact(Month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first)) return;
        From = first.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        To = first.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private string Query()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(From)) parts.Add("from=" + Uri.EscapeDataString(From));
        if (!string.IsNullOrWhiteSpace(To)) parts.Add("to=" + Uri.EscapeDataString(To));
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    public sealed record SummaryBody(int Classes, int Learners, decimal AttendancePercent);
    private sealed record Envelope(SummaryBody? Item);
}
