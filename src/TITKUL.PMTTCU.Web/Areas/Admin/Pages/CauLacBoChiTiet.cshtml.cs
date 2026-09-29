using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class CauLacBoChiTietModel(BackendApiClient api) : PageModel
{
    public ClubItem? Club { get; private set; }
    public IReadOnlyList<ClubSession> Sessions { get; private set; } = [];
    public IReadOnlyList<ClubSession> MonthSessions { get; private set; } = [];
    public IReadOnlyList<CalendarWeek> CalendarWeeks { get; private set; } = [];
    public DateOnly CalendarMonth { get; private set; }
    public IReadOnlyList<StaffItem> Staff { get; private set; } = [];
    public bool CanManage { get; private set; }
    public string? Error { get; private set; }
    public string? Message { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, string? month) => await LoadAsync(id, ParseMonth(month));

    public async Task<IActionResult> OnPostCreateAsync(Guid id, string title, string startAt, string endAt, string? location, Guid? facilitatorId, string? note, string? month)
    {
        if (!CanManageUser()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (!TryLocalTime(startAt, out var start) || !TryLocalTime(endAt, out var end))
        {
            Error = "Nhập thời gian bắt đầu và kết thúc hợp lệ.";
            return await LoadAsync(id, ParseMonth(month));
        }
        using var response = await api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/clubs/{id}/sessions", token,
            new { title, startAt = start, endAt = end, location, facilitatorId, note });
        if (response?.IsSuccessStatusCode != true) Error = "Không tạo được buổi. Kiểm tra nội dung, thời gian và người phụ trách.";
        else Message = "Đã thêm buổi sinh hoạt.";
        return await LoadAsync(id, ParseMonth(month));
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, Guid sessionId, string? month)
    {
        if (!CanManageUser()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/club-sessions/{sessionId}/cancel", token, new { });
        if (response?.IsSuccessStatusCode != true) Error = "Không hủy được buổi sinh hoạt.";
        else Message = "Đã hủy buổi sinh hoạt.";
        return await LoadAsync(id, ParseMonth(month));
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, Guid sessionId, string title, string startAt, string endAt, string? location, Guid? facilitatorId, string? note, string? month)
    {
        if (!CanManageUser()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (!TryLocalTime(startAt, out var start) || !TryLocalTime(endAt, out var end))
        {
            Error = "Nhập thời gian bắt đầu và kết thúc hợp lệ.";
            return await LoadAsync(id, ParseMonth(month));
        }
        using var response = await api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/club-sessions/{sessionId}", token,
            new { title, startAt = start, endAt = end, location, facilitatorId, note });
        if (response?.IsSuccessStatusCode != true) Error = "Không cập nhật được buổi sinh hoạt.";
        else Message = "Đã cập nhật buổi sinh hoạt.";
        return await LoadAsync(id, ParseMonth(month));
    }

    private async Task<IActionResult> LoadAsync(Guid id, DateOnly month)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanManage = CanManageUser();
        var clubResponse = await api.GetJsonAsync<ItemEnvelope<ClubItem>>($"/api/v1/admin/clubs/{id}", token);
        Club = clubResponse?.Item;
        if (Club is null) { Error ??= "Không tìm thấy câu lạc bộ."; return Page(); }
        var sessions = await api.GetJsonAsync<ListEnvelope<ClubSession>>($"/api/v1/admin/clubs/{id}/sessions", token);
        Sessions = sessions?.Items ?? [];
        CalendarMonth = new DateOnly(month.Year, month.Month, 1);
        MonthSessions = Sessions
            .Where(session =>
            {
                var localDate = DateOnly.FromDateTime(session.StartAt.ToOffset(TimeSpan.FromHours(7)).DateTime);
                return localDate.Year == CalendarMonth.Year && localDate.Month == CalendarMonth.Month;
            })
            .OrderBy(session => session.StartAt)
            .ToArray();
        CalendarWeeks = BuildCalendar(CalendarMonth, MonthSessions);
        var staff = await api.GetJsonAsync<ListEnvelope<StaffItem>>("/api/v1/admin/education/staff", token);
        Staff = staff?.Items ?? [];
        return Page();
    }

    private static bool TryLocalTime(string input, out DateTimeOffset value) =>
        DateTimeOffset.TryParse(input, out value) && (value = new DateTimeOffset(value.DateTime, TimeSpan.FromHours(7))) != default;
    private static DateOnly ParseMonth(string? month)
    {
        if (month is { Length: 7 }
            && month[4] == '-'
            && int.TryParse(month.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            && int.TryParse(month.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var monthNumber)
            && year is >= 1 and <= 9999
            && monthNumber is >= 1 and <= 12)
            return new DateOnly(year, monthNumber, 1);
        var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
        return new DateOnly(today.Year, today.Month, 1);
    }

    private static IReadOnlyList<CalendarWeek> BuildCalendar(DateOnly month, IReadOnlyList<ClubSession> sessions)
    {
        var firstDay = month.AddDays(-(((int)month.DayOfWeek + 6) % 7));
        var byDate = sessions
            .GroupBy(session => DateOnly.FromDateTime(session.StartAt.ToOffset(TimeSpan.FromHours(7)).DateTime))
            .ToDictionary(group => group.Key, group => group.OrderBy(session => session.StartAt).ToArray());
        return Enumerable.Range(0, 6)
            .Select(week => new CalendarWeek(Enumerable.Range(0, 7)
                .Select(dayOffset => firstDay.AddDays(week * 7 + dayOffset))
                .Select(date => new CalendarDay(date, date.Month == month.Month, byDate.GetValueOrDefault(date, [])))
                .ToArray()))
            .ToArray();
    }

    private bool HasView() => Has("education.manage") || Has("education.view");
    private bool CanManageUser() => Has("education.manage");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record ClubItem(Guid Id, string Code, string Name, string? Location, string? RegularSchedule, string Status);
    public sealed record ClubSession(Guid Id, Guid ClubId, string Title, DateTimeOffset StartAt, DateTimeOffset EndAt, string? Location, Guid? FacilitatorId, string? FacilitatorName, string Status, string? Note);
    public sealed record CalendarDay(DateOnly Date, bool InMonth, IReadOnlyList<ClubSession> Sessions);
    public sealed record CalendarWeek(IReadOnlyList<CalendarDay> Days);
    public sealed record StaffItem(Guid Id, string Username);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
