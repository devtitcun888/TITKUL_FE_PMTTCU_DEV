using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class KhaoSatXemTruocModel : PageModel
{
    private readonly BackendApiClient _api;
    public KhaoSatXemTruocModel(BackendApiClient api) => _api = api;
    public SurveyHead? Survey { get; private set; }
    public IReadOnlyList<QuestionItem> Questions { get; private set; } = [];
    public IReadOnlyList<OptionItem> Options { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!(Has("survey.view") || Has("survey.manage"))) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = await _api.GetJsonAsync<DetailEnvelope>("/api/v1/admin/surveys/" + id, token);
        Survey = body?.Item?.Survey;
        Questions = body?.Item?.Questions ?? [];
        Options = body?.Item?.Options ?? [];
        return Page();
    }

    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    public sealed record SurveyHead(Guid Id, string Title, string? Summary);
    public sealed record QuestionItem(Guid Id, string Text, string Type, bool Required, short? ScaleMin, short? ScaleMax);
    public sealed record OptionItem(Guid QuestionId, string Text);
    public sealed record Detail(SurveyHead? Survey, IReadOnlyList<QuestionItem>? Questions, IReadOnlyList<OptionItem>? Options);
    private sealed record DetailEnvelope(Detail? Item);
}
