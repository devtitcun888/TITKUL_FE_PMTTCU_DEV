using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class LopHocModel : PageModel
{
    private readonly BackendApiClient _api;
    public LopHocModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<ClassItem> Items { get; private set; } = [];
    public IReadOnlyList<OptionItem> Programs { get; private set; } = [];
    public IReadOnlyList<OptionItem> Rooms { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    public int PageNumber { get; private set; } = 1;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public string? StatusFilter { get; private set; }
    public Guid? ProgramFilter { get; private set; }

    [BindProperty] public Guid? ProgramId { get; set; }
    [BindProperty] public Guid? RoomId { get; set; }
    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public DateOnly? StartDate { get; set; }
    [BindProperty] public DateOnly? EndDate { get; set; }
    [BindProperty] public int Capacity { get; set; } = 20;

    public async Task<IActionResult> OnGetAsync(string? q, string? status, Guid? programId, int page = 1) =>
        await LoadAsync(q, status, programId, page);

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/classes", token, new
        {
            programId = ProgramId,
            roomId = RoomId,
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
            return await LoadAsync(null, null, null, 1);
        }

        return Redirect("/admin/lop-hoc");
    }

    private bool HasManage() => Has("education.manage");
    private bool HasView() => Has("education.manage") || Has("education.view") || Has("report.own_classes.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(string? q, string? status, Guid? programId, int page)
    {
        CanManage = HasManage();
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        Query = q;
        StatusFilter = status;
        ProgramFilter = programId;
        PageNumber = page < 1 ? 1 : page;
        var query = $"/api/v1/admin/classes?page={PageNumber}&pageSize=20";
        if (!string.IsNullOrWhiteSpace(q)) query += "&q=" + Uri.EscapeDataString(q);
        if (!string.IsNullOrWhiteSpace(status)) query += "&status=" + Uri.EscapeDataString(status);
        if (programId is Guid filter) query += "&programId=" + filter;
        var classes = await _api.GetJsonAsync<ListEnvelope<ClassItem>>(query, token);
        var programs = await _api.GetJsonAsync<ListEnvelope<OptionItem>>("/api/v1/admin/programs?pageSize=100", token);
        var rooms = await _api.GetJsonAsync<ListEnvelope<OptionItem>>("/api/v1/admin/rooms?pageSize=100", token);
        if (classes is null) ErrorMessage ??= "Không tải được danh sách lớp.";
        Items = classes?.Items ?? [];
        Total = classes?.Total ?? Items.Count;
        Programs = programs?.Items ?? [];
        Rooms = rooms?.Items ?? [];
        return Page();
    }

    public sealed record ClassItem(Guid Id, string Code, string Name, string Status, DateOnly StartDate, DateOnly EndDate, int Capacity);
    public sealed record OptionItem(Guid Id, string Code, string Name);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int Total);
}
