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
    public DateOnly MonthStart { get; private set; }
    public DateOnly CalendarStart { get; private set; }
    public bool MonthView { get; private set; }
    public bool CanManage => Has("education.manage");
    public IReadOnlyList<SessionItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? week, string? month, string? view)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (string.IsNullOrWhiteSpace(token)) return Redirect("/admin/dang-nhap");
        MonthView = string.Equals(view, "month", StringComparison.OrdinalIgnoreCase);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var parsedWeek = DateOnly.TryParse(week, out var weekDay) ? weekDay : today;
        WeekStart = EducationUi.MondayOf(parsedWeek);
        MonthStart = DateOnly.TryParseExact(month, "yyyy-MM", out var monthDate)
            ? new DateOnly(monthDate.Year, monthDate.Month, 1)
            : new DateOnly(today.Year, today.Month, 1);
        CalendarStart = EducationUi.MondayOf(MonthStart);
        var rangeStart = MonthView ? MonthStart : WeekStart;
        var rangeDays = MonthView ? DateTime.DaysInMonth(MonthStart.Year, MonthStart.Month) : 7;
        var from = new DateTimeOffset(rangeStart.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7));
        var to = from.AddDays(rangeDays);
        var path = $"/api/v1/admin/sessions?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&pageSize=100";
        var body = await _api.GetJsonAsync<ListEnvelope<SessionItem>>(path, token);
        if (body is null) ErrorMessage = "Không tải được lịch học.";
        Items = body?.Items ?? [];
        return Page();
    }

    public async Task<IActionResult> OnPostMoveAsync(Guid id, string? targetDay, DateTimeOffset startAt, DateTimeOffset endAt, Guid? roomId, string title, string mode, string? meetingUrl, string? note)
    {
        if (!Has("education.manage")) return new JsonResult(new { error = "Không có quyền sửa lịch." }) { StatusCode = 403 };
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (string.IsNullOrWhiteSpace(token)) return new JsonResult(new { error = "Phiên đăng nhập đã hết hạn." }) { StatusCode = 401 };
        if (!DateOnly.TryParse(targetDay, out var day) || endAt <= startAt) return BadRequest();

        var localStart = startAt.ToOffset(TimeSpan.FromHours(7));
        var newStart = new DateTimeOffset(day.ToDateTime(TimeOnly.FromDateTime(localStart.DateTime)), TimeSpan.FromHours(7));
        var newEnd = newStart.Add(endAt - startAt);
        using var response = await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/sessions/{id}", token, new
        {
            title,
            roomId,
            startAt = newStart,
            endAt = newEnd,
            mode,
            meetingUrl,
            note
        });
        return response?.IsSuccessStatusCode == true
            ? new JsonResult(new { success = true })
            : new JsonResult(new { error = "Không thể chuyển buổi học đến ngày này." }) { StatusCode = 400 };
    }

    public IReadOnlyList<DateOnly> CalendarDays() =>
        Enumerable.Range(0, 42).Select(offset => CalendarStart.AddDays(offset)).ToArray();

    public IReadOnlyList<SessionItem> ForDay(DateOnly day) =>
        Items.Where(item => DateOnly.FromDateTime(item.StartAt.ToOffset(TimeSpan.FromHours(7)).DateTime) == day)
            .OrderBy(item => item.StartAt)
            .ToArray();

    private bool HasView() =>
        Has("education.manage") || Has("education.view") || Has("report.own_classes.view");

    private bool Has(string permission) =>
        (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    public sealed record SessionItem(Guid Id, Guid ClassId, Guid? RoomId, string Title, DateTimeOffset StartAt, DateTimeOffset EndAt, string Mode, string? MeetingUrl, string Status, string? Note, string? ClassCode, string? ClassName);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
