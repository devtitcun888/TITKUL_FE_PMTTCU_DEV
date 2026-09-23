using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class KhaoSatThietKeModel : PageModel
{
    private readonly BackendApiClient _api;
    public KhaoSatThietKeModel(BackendApiClient api) => _api = api;
    public SurveyHead? Survey { get; private set; }
    public IReadOnlyList<QuestionItem> Questions { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    [BindProperty] public string Text { get; set; } = "";
    [BindProperty] public string Type { get; set; } = "SINGLE";
    [BindProperty] public bool Required { get; set; }
    [BindProperty] public string? Choices { get; set; }
    [BindProperty] public short ScaleMin { get; set; } = 1;
    [BindProperty] public short ScaleMax { get; set; } = 5;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostQuestionAsync(Guid id)
    {
        if (!Has("survey.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var options = (Choices ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => new { text = line, value = line }).ToArray();
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/surveys/" + id + "/questions", token, new
        {
            text = Text,
            type = Type,
            required = Required,
            scaleMin = Type == "SCALE" ? ScaleMin : (short?)null,
            scaleMax = Type == "SCALE" ? ScaleMax : (short?)null,
            options = Type is "SINGLE" or "MULTIPLE" ? options : null
        });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không thêm được câu. Một chọn/nhiều chọn cần ít nhất 2 lựa chọn. Tối đa 30 câu. Đã có phiếu thì không sửa cấu trúc.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnGetQrAsync(Guid id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync("/api/v1/admin/surveys/" + id + "/qr", token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, "image/png", "khao-sat.png");
    }

    public async Task<IActionResult> OnPostOpenAsync(Guid id)
    {
        if (!Has("survey.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/surveys/" + id + "/open", token, new { });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Chưa mở được. Cần ít nhất một câu hỏi.";
        return await LoadAsync(id);
    }

    private async Task<IActionResult> LoadAsync(Guid id)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanManage = Has("survey.manage");
        var body = await _api.GetJsonAsync<DetailEnvelope>("/api/v1/admin/surveys/" + id, token);
        Survey = body?.Item?.Survey;
        Questions = body?.Item?.Questions ?? [];
        if (Survey is null) ErrorMessage ??= "Không tìm thấy đợt khảo sát.";
        return Page();
    }

    private bool HasView() => Has("survey.view") || Has("survey.manage");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record SurveyHead(Guid Id, string Title, string Code, string Status);
    public sealed record QuestionItem(Guid Id, string Text, string Type, bool Required);
    public sealed record Detail(SurveyHead? Survey, IReadOnlyList<QuestionItem>? Questions);
    private sealed record DetailEnvelope(Detail? Item);
}
