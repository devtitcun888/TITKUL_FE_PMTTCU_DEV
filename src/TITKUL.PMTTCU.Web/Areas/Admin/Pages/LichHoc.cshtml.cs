using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class LichHocModel : PageModel
{
    private readonly BackendApiClient _api;
    public LichHocModel(BackendApiClient api) => _api = api;
    public DateOnly WeekStart { get; private set; }
    public IReadOnlyList<SessionItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? week)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (string.IsNullOrWhiteSpace(token)) return Redirect("/admin/dang-nhap");
        var parsed = DateOnly.TryParse(week, out var day) ? day : DateOnly.FromDateTime(DateTime.Today);
        WeekStart = EducationUi.MondayOf(parsed);
        var from = new DateTimeOffset(WeekStart.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7));
        var to = from.AddDays(7);
        var path = $"/api/v1/admin/sessions?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&pageSize=100";
        var body = await _api.GetJsonAsync<ListEnvelope<SessionItem>>(path, token);
        if (body is null) ErrorMessage = "Không tải được lịch học.";
        Items = body?.Items ?? [];
        return Page();
    }

    public IReadOnlyList<SessionItem> ForDay(DateOnly day) =>
        Items.Where(item => DateOnly.FromDateTime(item.StartAt.ToOffset(TimeSpan.FromHours(7)).DateTime) == day)
            .OrderBy(item => item.StartAt)
            .ToArray();

    private bool HasView() =>
        Has("education.manage") || Has("education.view") || Has("report.own_classes.view");

    private bool Has(string permission) =>
        (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    public sealed record SessionItem(Guid Id, Guid ClassId, string Title, DateTimeOffset StartAt, DateTimeOffset EndAt, string Mode, string Status, string? ClassCode, string? ClassName);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
