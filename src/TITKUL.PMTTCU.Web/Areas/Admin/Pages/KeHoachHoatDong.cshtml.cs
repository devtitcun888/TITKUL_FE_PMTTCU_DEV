using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class KeHoachHoatDongModel : PageModel
{
    private readonly BackendApiClient _api;
    public KeHoachHoatDongModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<PlanItem> Plans { get; private set; } = [];
    public IReadOnlyList<ActivityItem> Activities { get; private set; } = [];
    public PlanItem? SelectedPlan { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }

    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty(SupportsGet = true)] public int? Year { get; set; }
    [BindProperty] public int? FromYear { get; set; }
    [BindProperty] public int? ToYear { get; set; }
    [BindProperty] public int? PlanYear { get; set; }
    [BindProperty] public string? PriorityActivities { get; set; }
    [BindProperty] public string? RegularActivities { get; set; }
    [BindProperty] public string? Approver { get; set; }
    [BindProperty] public string? Planner { get; set; }
    [BindProperty] public Guid? ActivityId { get; set; }
    [BindProperty] public string ActivityAndTime { get; set; } = "";
    [BindProperty] public string? Audience { get; set; }
    [BindProperty] public string? ExpectedResult { get; set; }
    [BindProperty] public string? Resources { get; set; }
    [BindProperty] public string? ActivityName { get; set; }
    [BindProperty] public string? TimeRange { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string? Objective { get; set; }
    [BindProperty] public string? Schedule { get; set; }
    [BindProperty] public string? ResponsiblePartner { get; set; }
    [BindProperty] public string? DetailedResources { get; set; }
    [BindProperty] public string? DetailedExpectedResult { get; set; }
    [BindProperty] public string? RisksAndMitigation { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? activityId) => await LoadAsync(id, activityId);

    public async Task<IActionResult> OnPostSavePlanAsync()
    {
        if (!CanManageUser()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { fromYear = FromYear, toYear = ToYear, planYear = PlanYear, priorityActivities = PriorityActivities,
            regularActivities = RegularActivities, approver = Approver, planner = Planner };
        var response = Id is Guid id
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/activity-plans/{id}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/activity-plans", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được kế hoạch. Kiểm tra năm bắt đầu và năm kết thúc.";
            return await LoadAsync(Id, null);
        }

        var envelope = await response.Content.ReadFromJsonAsync<ItemEnvelope<PlanItem>>();
        return envelope?.Item is PlanItem item ? Redirect($"/admin/ke-hoach-hoat-dong?id={item.Id}") : Redirect("/admin/ke-hoach-hoat-dong");
    }

    public async Task<IActionResult> OnPostSaveActivityAsync()
    {
        if (!CanManageUser()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (Id is not Guid planId) return Redirect("/admin/ke-hoach-hoat-dong");
        var body = new { activityAndTime = ActivityAndTime, audience = Audience, expectedResult = ExpectedResult, resources = Resources,
            activityName = ActivityName, timeRange = TimeRange, location = Location, objective = Objective, schedule = Schedule,
            responsiblePartner = ResponsiblePartner, detailedResources = DetailedResources,
            detailedExpectedResult = DetailedExpectedResult, risksAndMitigation = RisksAndMitigation };
        var response = ActivityId is Guid activityId
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/activity-plan-activities/{activityId}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/activity-plans/{planId}/activities", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được hoạt động. Cần nhập cột Hoạt động và thời gian thực hiện.";
            return await LoadAsync(planId, ActivityId);
        }

        return Redirect($"/admin/ke-hoach-hoat-dong?id={planId}");
    }

    public async Task<IActionResult> OnPostDeleteActivityAsync(Guid id, Guid activityId)
    {
        if (!CanManageUser()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/activity-plan-activities/{activityId}", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không xóa được hoạt động.";
            return await LoadAsync(id, null);
        }

        return Redirect($"/admin/ke-hoach-hoat-dong?id={id}");
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        if (!CanManageUser()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/activity-plans/{id}/approve", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không cập nhật được trạng thái kế hoạch.";
            return await LoadAsync(id, null);
        }

        return Redirect($"/admin/ke-hoach-hoat-dong?id={id}");
    }

    public async Task<IActionResult> OnGetExportAsync(Guid id)
    {
        if (!CanManageUser()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync($"/api/v1/admin/activity-plans/{id}/export", token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName ?? "ke-hoach-hoat-dong.xlsx");
    }

    private bool CanManageUser() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("education.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(Guid? id, Guid? activityId)
    {
        CanManage = CanManageUser();
        if (!CanManage) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var listPath = "/api/v1/admin/activity-plans?pageSize=100" + (Year is int year ? "&year=" + year : "");
        var list = await _api.GetJsonAsync<PagedEnvelope<PlanItem>>(listPath, token);
        Plans = list?.Items ?? [];
        if (id is Guid planId)
        {
            var detail = await _api.GetJsonAsync<PlanDetailEnvelope>($"/api/v1/admin/activity-plans/{planId}", token);
            if (detail?.Item is null)
            {
                ErrorMessage ??= "Không tải được kế hoạch hoạt động.";
                return Page();
            }
            SelectedPlan = detail.Item;
            Id = planId;
            Activities = detail.Activities ?? [];
            FromYear = detail.Item.FromYear;
            ToYear = detail.Item.ToYear;
            PlanYear = detail.Item.PlanYear;
            PriorityActivities = detail.Item.PriorityActivities;
            RegularActivities = detail.Item.RegularActivities;
            Approver = detail.Item.Approver;
            Planner = detail.Item.Planner;
            if (activityId is Guid selectedActivityId)
            {
                var selected = Activities.FirstOrDefault(item => item.Id == selectedActivityId);
                if (selected is not null) LoadActivity(selected);
            }
        }

        return Page();
    }

    private void LoadActivity(ActivityItem item)
    {
        ActivityId = item.Id;
        ActivityAndTime = item.ActivityAndTime;
        Audience = item.Audience;
        ExpectedResult = item.ExpectedResult;
        Resources = item.Resources;
        ActivityName = item.ActivityName;
        TimeRange = item.TimeRange;
        Location = item.Location;
        Objective = item.Objective;
        Schedule = item.Schedule;
        ResponsiblePartner = item.ResponsiblePartner;
        DetailedResources = item.DetailedResources;
        DetailedExpectedResult = item.DetailedExpectedResult;
        RisksAndMitigation = item.RisksAndMitigation;
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage? response)
    {
        if (response is null) return null;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorBody>();
            return string.IsNullOrWhiteSpace(error?.Message) ? null : error.Message;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public sealed record PlanItem(Guid Id, int? FromYear, int? ToYear, int? PlanYear, string? PriorityActivities,
        string? RegularActivities, string? Approver, string? Planner, string Status);
    public sealed record ActivityItem(Guid Id, Guid PlanId, int Sequence, string ActivityAndTime, string? Audience,
        string? ExpectedResult, string? Resources, string? ActivityName, string? TimeRange, string? Location,
        string? Objective, string? Schedule, string? ResponsiblePartner, string? DetailedResources,
        string? DetailedExpectedResult, string? RisksAndMitigation);
    private sealed record PagedEnvelope<T>(IReadOnlyList<T>? Items, int Page, int PageSize, int Total);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record PlanDetailEnvelope(PlanItem? Item, IReadOnlyList<ActivityItem>? Activities);
    private sealed record ApiErrorBody(string? Code, string? Message);
}
