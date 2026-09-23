using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class HocVienChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public HocVienChiTietModel(BackendApiClient api) => _api = api;
    public LearnerDetail? Item { get; private set; }
    public IReadOnlyList<LearnerClass> Classes { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = await _api.GetJsonAsync<DetailEnvelope>($"/api/v1/admin/learners/{id}", token);
        Item = body?.Item;
        Classes = body?.Classes ?? [];
        if (Item is null) ErrorMessage = "Không tìm thấy học viên trong phạm vi của bạn.";
        return Page();
    }

    private bool HasView() => Has("learner.view") || Has("learner.manage");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record LearnerDetail(Guid Id, string FullName, string Phone, string CccdMasked, DateOnly BirthDate, int Age, string Gender, string HamletName, string? Note, string Status);
    public sealed record LearnerClass(Guid EnrollmentId, string RegistrationCode, Guid ClassId, string ClassCode, string ClassName, string EnrollmentStatus, string ClassStatus, DateTimeOffset RegisteredAt);
    private sealed record DetailEnvelope(LearnerDetail? Item, IReadOnlyList<LearnerClass>? Classes);
}
