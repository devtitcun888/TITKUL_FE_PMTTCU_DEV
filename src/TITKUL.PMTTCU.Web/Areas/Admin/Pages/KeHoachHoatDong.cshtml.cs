using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
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
    public bool ShowPlanModal { get; private set; }
    public bool ShowActivityModal { get; private set; }
    public string? StatusFilter { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public string? ActivitySort { get; private set; }
    public string ActivityDir { get; private set; } = "asc";
    public int TotalAll { get; private set; }
    public int TotalDraft { get; private set; }
    public int TotalApproved { get; private set; }
    public bool FiltersActive => Year is not null || StatusFilter is not null;

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

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? activityId, string? status, string? sort, string? dir, string? asort, string? adir, bool create = false, bool editPlan = false, bool createActivity = false)
    {
        var page = await LoadAsync(id, activityId, status, sort, dir, asort, adir);
        ShowPlanModal = create || (editPlan && SelectedPlan is not null);
        ShowActivityModal = SelectedPlan is not null && (ActivityId.HasValue || createActivity);
        return page;
    }

    public string ListUrl(int? year = null, string? status = null, string? sort = null, string? dir = null, Guid? id = null, bool omitId = false, bool clearYear = false)
    {
        var query = new Dictionary<string, string?>();
        var nextYear = clearYear ? null : year ?? Year;
        var nextStatus = status ?? StatusFilter;
        if (status == "") nextStatus = null;
        var nextId = omitId ? null : id ?? Id;
        if (nextYear is int y) query["Year"] = y.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(nextStatus)) query["status"] = nextStatus;
        CmsListSort.Append(query, sort ?? Sort, dir ?? Dir);
        if (!string.IsNullOrWhiteSpace(ActivitySort))
        {
            query["asort"] = ActivitySort;
            if (ActivityDir == "desc") query["adir"] = "desc";
        }
        if (nextId is Guid planId) query["id"] = planId.ToString();
        return QueryHelpers.AddQueryString("/admin/ke-hoach-hoat-dong", query);
    }

    public string SortUrl(string column) => ListUrl(sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public string ActivitySortUrl(string column)
    {
        var query = new Dictionary<string, string?>();
        if (Year is int y) query["Year"] = y.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(StatusFilter)) query["status"] = StatusFilter;
        CmsListSort.Append(query, Sort, Dir);
        var next = CmsListSort.Normalize(column, "seq", "activity", "audience");
        var nextDir = CmsListSort.NextDir(ActivitySort, column, ActivityDir);
        if (next is not null)
        {
            query["asort"] = next;
            if (nextDir == "desc") query["adir"] = "desc";
        }
        if (Id is Guid planId) query["id"] = planId.ToString();
        return QueryHelpers.AddQueryString("/admin/ke-hoach-hoat-dong", query);
    }

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
            ShowPlanModal = true;
            return await LoadAsync(Id, null, StatusFilter, Sort, Dir, ActivitySort, ActivityDir);
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
            ShowActivityModal = true;
            return await LoadAsync(planId, ActivityId, StatusFilter, Sort, Dir, ActivitySort, ActivityDir);
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
            return await LoadAsync(id, null, StatusFilter, Sort, Dir, ActivitySort, ActivityDir);
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
            return await LoadAsync(id, null, StatusFilter, Sort, Dir, ActivitySort, ActivityDir);
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

    private async Task<IActionResult> LoadAsync(Guid? id, Guid? activityId, string? status, string? sort, string? dir, string? asort, string? adir)
    {
        CanManage = CanManageUser();
        if (!CanManage) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        StatusFilter = status is "APPROVED" or "DRAFT" ? status : null;
        Sort = CmsListSort.Normalize(sort, "year", "range", "status", "planner", "approver");
        Dir = CmsListSort.Dir(dir);
        ActivitySort = CmsListSort.Normalize(asort, "seq", "activity", "audience");
        ActivityDir = CmsListSort.Dir(adir);
        var listPath = "/api/v1/admin/activity-plans?pageSize=100" + (Year is int year ? "&year=" + year : "");
        var list = await _api.GetJsonAsync<PagedEnvelope<PlanItem>>(listPath, token);
        var all = list?.Items ?? [];
        if (list is null && string.IsNullOrEmpty(ErrorMessage)) ErrorMessage = "Không tải được danh sách kế hoạch.";
        TotalAll = all.Count;
        TotalApproved = all.Count(item => item.Status == "APPROVED");
        TotalDraft = all.Count(item => item.Status != "APPROVED");
        IEnumerable<PlanItem> filtered = all;
        if (StatusFilter == "APPROVED") filtered = filtered.Where(item => item.Status == "APPROVED");
        else if (StatusFilter == "DRAFT") filtered = filtered.Where(item => item.Status != "APPROVED");
        Plans = filtered.ToArray();
        if (Sort is not null)
        {
            Plans = CmsListSort.Order(Plans, Dir, Sort switch
            {
                "range" => Plans.OrderBy(item => item.FromYear ?? 0).ThenBy(item => item.ToYear ?? 0),
                "status" => Plans.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                "planner" => Plans.OrderBy(item => item.Planner ?? "", StringComparer.CurrentCultureIgnoreCase),
                "approver" => Plans.OrderBy(item => item.Approver ?? "", StringComparer.CurrentCultureIgnoreCase),
                _ => Plans.OrderBy(item => item.PlanYear ?? 0)
            });
        }
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
            if (ActivitySort is not null)
            {
                Activities = CmsListSort.Order(Activities, ActivityDir, ActivitySort switch
                {
                    "activity" => Activities.OrderBy(item => item.ActivityAndTime, StringComparer.CurrentCultureIgnoreCase),
                    "audience" => Activities.OrderBy(item => item.Audience ?? "", StringComparer.CurrentCultureIgnoreCase),
                    _ => Activities.OrderBy(item => item.Sequence)
                });
            }
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
