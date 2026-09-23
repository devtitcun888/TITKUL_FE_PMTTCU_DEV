using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class HocVienModel : PageModel
{
    private readonly BackendApiClient _api;
    public HocVienModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<LearnerItem> Items { get; private set; } = [];
    public IReadOnlyList<OptionItem> Classes { get; private set; } = [];
    public IReadOnlyList<HamletItem> Hamlets { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public int PageNumber { get; private set; } = 1;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public Guid? ClassFilter { get; private set; }
    public Guid? HamletFilter { get; private set; }
    public int? AgeFrom { get; private set; }
    public int? AgeTo { get; private set; }
    public bool CanExport { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? q, Guid? classId, Guid? hamletId, int? ageFrom, int? ageTo, int page = 1)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        CanExport = Has("learner.export");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        Query = q;
        ClassFilter = classId;
        HamletFilter = hamletId;
        AgeFrom = ageFrom;
        AgeTo = ageTo;
        PageNumber = page < 1 ? 1 : page;
        var path = $"/api/v1/admin/learners?page={PageNumber}&pageSize=20";
        if (!string.IsNullOrWhiteSpace(q)) path += "&q=" + Uri.EscapeDataString(q);
        if (classId is Guid cls) path += "&classId=" + cls;
        if (hamletId is Guid hamlet) path += "&hamletId=" + hamlet;
        if (ageFrom is int from) path += "&ageFrom=" + from;
        if (ageTo is int to) path += "&ageTo=" + to;
        var list = await _api.GetJsonAsync<ListEnvelope<LearnerItem>>(path, token);
        var classes = await _api.GetJsonAsync<ListEnvelope<OptionItem>>("/api/v1/admin/classes?pageSize=100", token);
        var hamlets = await _api.GetJsonAsync<ListEnvelope<HamletItem>>("/api/v1/admin/hamlets", token);
        Items = list?.Items ?? [];
        Total = list?.Total ?? 0;
        Classes = classes?.Items ?? [];
        Hamlets = hamlets?.Items ?? [];
        if (list is null) ErrorMessage = "Không tải được danh sách học viên.";
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync(string? q, Guid? classId, Guid? hamletId, int? ageFrom, int? ageTo)
    {
        if (!Has("learner.export")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var path = "/api/v1/admin/learners/export?";
        if (!string.IsNullOrWhiteSpace(q)) path += "q=" + Uri.EscapeDataString(q) + "&";
        if (classId is Guid cls) path += "classId=" + cls + "&";
        if (hamletId is Guid hamlet) path += "hamletId=" + hamlet + "&";
        if (ageFrom is int from) path += "ageFrom=" + from + "&";
        if (ageTo is int to) path += "ageTo=" + to + "&";
        var file = await _api.GetFileAsync(path.TrimEnd('&'), token);
        if (file.Bytes is null)
        {
            ErrorMessage = "Không xuất được Excel.";
            return await OnGetAsync(q, classId, hamletId, ageFrom, ageTo);
        }

        return File(file.Bytes, file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName ?? "hoc-vien.xlsx");
    }

    private bool HasView() => Has("learner.view") || Has("learner.manage");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record LearnerItem(Guid Id, string FullName, string Phone, DateOnly BirthDate, int Age, string Gender, string HamletName, string Status);
    public sealed record OptionItem(Guid Id, string Code, string Name);
    public sealed record HamletItem(Guid Id, string Code, string Name);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int Total);
}
