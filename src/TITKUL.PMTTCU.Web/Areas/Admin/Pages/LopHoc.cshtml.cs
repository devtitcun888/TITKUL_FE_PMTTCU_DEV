using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class LopHocModel : PageModel
{
    private static readonly int[] PageSizes = [10, 20, 50];
    private readonly BackendApiClient _api;
    public LopHocModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<ClassItem> Items { get; private set; } = [];
    public IReadOnlyList<OptionItem> Programs { get; private set; } = [];
    public IReadOnlyList<OptionItem> Rooms { get; private set; } = [];
    public IReadOnlyList<OptionItem> Clubs { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    public bool ShowModal { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public int TotalAll { get; private set; }
    public int TotalOpen { get; private set; }
    public int TotalLearning { get; private set; }
    public int TotalClosed { get; private set; }
    public string? Query { get; private set; }
    public string? StatusFilter { get; private set; }
    public Guid? ProgramFilter { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public bool FiltersActive => Query is not null || StatusFilter is not null || ProgramFilter is not null;
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(PageSize, 1)));
    public int FromItem => Total == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int ToItem => Math.Min(CurrentPage * PageSize, Total);

    [BindProperty] public Guid? ProgramId { get; set; }
    [BindProperty] public Guid? RoomId { get; set; }
    [BindProperty] public Guid? ClubId { get; set; }
    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public DateOnly? StartDate { get; set; }
    [BindProperty] public DateOnly? EndDate { get; set; }
    [BindProperty] public int Capacity { get; set; } = 20;

    public async Task<IActionResult> OnGetAsync(string? q, string? status, Guid? programId, string? sort, string? dir, bool create = false, int page = 1, int pageSize = 20)
    {
        ShowModal = create;
        return await LoadAsync(q, status, programId, page, pageSize, sort, dir);
    }

    public string ListUrl(int? page = null, int? pageSize = null, string? status = null, Guid? programId = null, string? q = null, string? sort = null, string? dir = null)
    {
        var query = new Dictionary<string, string?>();
        var nextQ = q ?? Query;
        var nextStatus = status ?? StatusFilter;
        if (status == "") nextStatus = null;
        var nextProgram = programId ?? ProgramFilter;
        var nextSize = pageSize ?? PageSize;
        var nextPage = page ?? CurrentPage;
        if (!string.IsNullOrWhiteSpace(nextQ)) query["q"] = nextQ;
        if (!string.IsNullOrWhiteSpace(nextStatus)) query["status"] = nextStatus;
        if (nextProgram is Guid program) query["programId"] = program.ToString();
        CmsListSort.Append(query, sort ?? Sort, dir ?? Dir);
        if (nextSize != 20) query["pageSize"] = nextSize.ToString(CultureInfo.InvariantCulture);
        if (nextPage > 1) query["page"] = nextPage.ToString(CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString("/admin/lop-hoc", query);
    }

    public string SortUrl(string column) => ListUrl(page: 1, sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/classes", token, new
        {
            programId = ProgramId,
            roomId = RoomId,
            clubId = ClubId,
            code = Code,
            name = Name,
            startDate = StartDate,
            endDate = EndDate,
            capacity = Capacity,
            @public = true
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được lớp. Kiểm tra mã, chương trình, sĩ số và ngày.";
            ShowModal = true;
            return await LoadAsync(null, null, null, 1, 20, null, null);
        }

        return Redirect("/admin/lop-hoc");
    }

    private bool HasManage() => Has("education.manage");
    private bool HasView() => Has("education.manage") || Has("education.view") || Has("report.own_classes.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(string? q, string? status, Guid? programId, int page, int pageSize, string? sort, string? dir)
    {
        CanManage = HasManage();
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        StatusFilter = status is "DU_KIEN" or "MO_DANG_KY" or "DANG_HOC" or "DONG" or "HUY" ? status : null;
        ProgramFilter = programId;
        Sort = CmsListSort.Normalize(sort, "code", "name", "status", "capacity");
        Dir = CmsListSort.Dir(dir);
        PageSize = PageSizes.Contains(pageSize) ? pageSize : 20;
        CurrentPage = page < 1 ? 1 : page;

        var allTask = CountAsync(token, null);
        var openTask = CountAsync(token, "MO_DANG_KY");
        var learningTask = CountAsync(token, "DANG_HOC");
        var closedTask = CountAsync(token, "DONG");
        var listTask = FetchAsync(token, CurrentPage, PageSize, StatusFilter, Query, ProgramFilter);
        var programsTask = _api.GetJsonAsync<ListEnvelope<OptionItem>>("/api/v1/admin/programs?pageSize=100", token);
        var roomsTask = _api.GetJsonAsync<ListEnvelope<OptionItem>>("/api/v1/admin/rooms?pageSize=100", token);
        var clubsTask = _api.GetJsonAsync<ListEnvelope<OptionItem>>("/api/v1/admin/clubs?pageSize=100&status=ACTIVE", token);
        await Task.WhenAll(allTask, openTask, learningTask, closedTask, listTask, programsTask, roomsTask, clubsTask);

        TotalAll = allTask.Result;
        TotalOpen = openTask.Result;
        TotalLearning = learningTask.Result;
        TotalClosed = closedTask.Result;
        var classes = listTask.Result;
        Items = classes.Items ?? [];
        Total = classes.Total;
        Programs = programsTask.Result?.Items ?? [];
        Rooms = roomsTask.Result?.Items ?? [];
        Clubs = clubsTask.Result?.Items ?? [];
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "code" => Items.OrderBy(item => item.Code, StringComparer.CurrentCultureIgnoreCase),
                "status" => Items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                "capacity" => Items.OrderBy(item => item.Capacity),
                _ => Items.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        return Page();
    }

    private async Task<int> CountAsync(string token, string? status)
    {
        var result = await FetchAsync(token, 1, 1, status, null, null);
        return result.Total;
    }

    private async Task<ListEnvelope<ClassItem>> FetchAsync(string token, int page, int pageSize, string? status, string? q, Guid? programId)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(status)) query["status"] = status;
        if (!string.IsNullOrWhiteSpace(q)) query["q"] = q;
        if (programId is Guid program) query["programId"] = program.ToString();
        var path = QueryHelpers.AddQueryString("/api/v1/admin/classes", query);
        var result = await _api.GetJsonAsync<ListEnvelope<ClassItem>>(path, token);
        if (result is null)
        {
            ErrorMessage ??= "Không tải được danh sách lớp.";
            return new ListEnvelope<ClassItem>([], 0);
        }
        return result;
    }

    public sealed record ClassItem(Guid Id, string Code, string Name, string Status, DateOnly StartDate, DateOnly EndDate, int Capacity);
    public sealed record OptionItem(Guid Id, string Code, string Name);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int Total);
}
