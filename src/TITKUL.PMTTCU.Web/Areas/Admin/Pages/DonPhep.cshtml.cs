using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class DonPhepModel : PageModel
{
    private readonly BackendApiClient _api;
    public DonPhepModel(BackendApiClient api) => _api = api;
    public Guid ClassId { get; private set; }
    public IReadOnlyList<EnrollmentOption> Enrollments { get; private set; } = [];
    public IReadOnlyList<SessionOption> Sessions { get; private set; } = [];
    public IReadOnlyList<LeaveItem> Items { get; private set; } = [];
    public string? Message { get; private set; }
    public string? Error { get; private set; }
    public bool CanManage => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Roles?.Any(role => role is "SUPER_ADMIN" or "CAN_BO" or "GIANG_VIEN") == true;
    [BindProperty] public Guid EnrollmentId { get; set; }
    [BindProperty] public Guid SessionId { get; set; }
    [BindProperty] public string Reason { get; set; } = "";
    [BindProperty] public Guid RequestId { get; set; }
    [BindProperty] public string DecisionNote { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid id) => await LoadAsync(id);

    public async Task<IActionResult> OnPostCreateAsync(Guid id)
    {
        if (!CanManage) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/classes/{id}/leave-requests", token,
            new { enrollmentId = EnrollmentId, sessionId = SessionId, reason = Reason });
        if (response is null || !response.IsSuccessStatusCode) Error = "Không tạo được đơn. Kiểm tra học viên, buổi học và lý do.";
        else Message = "Đã ghi nhận đơn xin phép chờ duyệt.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostDecisionAsync(Guid id, string decision)
    {
        if (!CanManage) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/leave-requests/{RequestId}/decision", token,
            new { decision, note = DecisionNote });
        if (response is null || !response.IsSuccessStatusCode) Error = "Không cập nhật được đơn; có thể đơn đã được xử lý hoặc điểm danh đã ghi có mặt.";
        else Message = decision == "APPROVE" ? "Đã duyệt và cập nhật điểm danh sang Có phép." : "Đã từ chối đơn.";
        return await LoadAsync(id);
    }

    private async Task<IActionResult> LoadAsync(Guid id)
    {
        ClassId = id;
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var enrollments = await _api.GetJsonAsync<ListEnvelope<EnrollmentOption>>($"/api/v1/admin/classes/{id}/enrollments?pageSize=100", token);
        var sessions = await _api.GetJsonAsync<ListEnvelope<SessionOption>>($"/api/v1/admin/classes/{id}/sessions?pageSize=100", token);
        var leave = await _api.GetJsonAsync<ListEnvelope<LeaveItem>>($"/api/v1/admin/classes/{id}/leave-requests", token);
        Enrollments = enrollments?.Items?.Where(item => item.Status != "HUY").ToArray() ?? [];
        Sessions = sessions?.Items?.Where(item => item.Status != "CANCELLED").ToArray() ?? [];
        Items = leave?.Items ?? [];
        return Page();
    }

    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];
    public sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    public sealed record EnrollmentOption(Guid Id, string FullName, string Status);
    public sealed record SessionOption(Guid Id, string Title, DateTimeOffset StartAt, string Status);
    public sealed record LeaveItem(Guid Id, Guid EnrollmentId, Guid SessionId, string Reason, string Status, string LearnerName, string SessionTitle, string? DecisionNote);
}
