using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class KhaoSatKetQuaModel : PageModel
{
    private readonly BackendApiClient _api;
    public KhaoSatKetQuaModel(BackendApiClient api) => _api = api;
    public Guid SurveyId { get; private set; }
    public ResultBody? Item { get; private set; }
    public bool CanExport { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!Has("survey.result.view")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        SurveyId = id;
        CanExport = Has("survey.export");
        var body = await _api.GetJsonAsync<Envelope>("/api/v1/admin/surveys/" + id + "/results", token);
        Item = body?.Item;
        return Page();
    }

    public async Task<IActionResult> OnGetXuatAsync(Guid id)
    {
        if (!Has("survey.export")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync("/api/v1/admin/surveys/" + id + "/responses/export", token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "khao-sat.xlsx");
    }

    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record OptionCount(string Text, int Count);
    public sealed record ScaleCount(decimal Value, int Count);
    public sealed record QuestionResult(string Text, string Type, IReadOnlyList<OptionCount> Options, IReadOnlyList<ScaleCount> Scales, IReadOnlyList<string> Texts);
    public sealed record ResultBody(int Responses, IReadOnlyList<QuestionResult> Questions);
    private sealed record Envelope(ResultBody? Item);
}
