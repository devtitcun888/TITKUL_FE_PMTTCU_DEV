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
    public IReadOnlyList<OptionItem> Options { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    [BindProperty] public string Text { get; set; } = "";
    [BindProperty] public string Type { get; set; } = "SINGLE";
    [BindProperty] public bool Required { get; set; }
    [BindProperty] public string? Choices { get; set; }
    [BindProperty] public short ScaleMin { get; set; } = 1;
    [BindProperty] public short ScaleMax { get; set; } = 5;
    [BindProperty] public string SurveyTitle { get; set; } = "";
    [BindProperty] public string SurveyStartAt { get; set; } = "";
    [BindProperty] public string SurveyEndAt { get; set; } = "";
    [BindProperty] public string? SurveySummary { get; set; }
    [BindProperty] public string? SurveyThanks { get; set; }
    [BindProperty] public bool RequirePhone { get; set; }
    [BindProperty] public bool LimitOnePerPhone { get; set; }

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
        var optionLines = (Choices ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var options = new List<object>();
        foreach (var line in optionLines)
        {
            var parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
            short? jump = null;
            if (parts.Length == 2 && short.TryParse(parts[1], out var order) && order is >= 2 and <= 30) jump = order;
            else if (parts.Length == 2)
            {
                ErrorMessage = "Câu đích rẽ nhánh phải là số thứ tự từ 2 đến 30.";
                return await LoadAsync(id);
            }
            options.Add(new { text = parts[0], value = parts[0], jumpToOrder = jump });
        }
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/surveys/" + id + "/questions", token, new
        {
            text = Text,
            type = Type,
            required = Required,
            scaleMin = Type == "SCALE" ? ScaleMin : (short?)null,
            scaleMax = Type == "SCALE" ? ScaleMax : (short?)null,
            options = Type is "SINGLE" or "MULTIPLE" ? options : null
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không thêm được câu. Một chọn/nhiều chọn cần ít nhất 2 lựa chọn. Tối đa 30 câu. Đã có phiếu thì không sửa cấu trúc.";
            return await LoadAsync(id);
        }
        Text = "";
        Type = "SINGLE";
        Required = false;
        Choices = null;
        ScaleMin = 1;
        ScaleMax = 5;
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

    public async Task<IActionResult> OnGetQrPreviewAsync(Guid id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync("/api/v1/admin/surveys/" + id + "/qr", token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, "image/png");
    }

    public async Task<IActionResult> OnPostOpenAsync(Guid id)
    {
        if (!Has("survey.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/surveys/" + id + "/open", token, new { });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Chưa mở được. Cần ít nhất một câu hỏi.";
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Chưa mở được. Cần ít nhất một câu hỏi.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostSettingsAsync(Guid id)
    {
        if (!Has("survey.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/surveys/" + id, token, new
        {
            title = SurveyTitle,
            summary = SurveySummary,
            startAt = ToOffset(SurveyStartAt),
            endAt = ToOffset(SurveyEndAt),
            thanks = SurveyThanks,
            requirePhone = RequirePhone || LimitOnePerPhone,
            limitOnePerPhone = LimitOnePerPhone
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được thiết lập. Không thể bật giới hạn số điện thoại sau khi khảo sát đã nhận phản hồi.";
            return await LoadAsync(id, preserveForm: true);
        }
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostCloseAsync(Guid id)
    {
        if (!Has("survey.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/surveys/" + id + "/close", token, new { });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Chưa đóng được khảo sát. Hãy kiểm tra trạng thái hiện tại.";
        return await LoadAsync(id);
    }

    private async Task<IActionResult> LoadAsync(Guid id, bool preserveForm = false)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanManage = Has("survey.manage");
        var body = await _api.GetJsonAsync<DetailEnvelope>("/api/v1/admin/surveys/" + id, token);
        Survey = body?.Item?.Survey;
        Questions = body?.Item?.Questions ?? [];
        Options = body?.Item?.Options ?? [];
        if (Survey is not null && !preserveForm)
        {
            SurveyTitle = Survey.Title;
            SurveyStartAt = LocalInput(Survey.StartAt);
            SurveyEndAt = LocalInput(Survey.EndAt);
            SurveySummary = Survey.Summary;
            SurveyThanks = Survey.Thanks;
            RequirePhone = Survey.RequirePhone;
            LimitOnePerPhone = Survey.LimitOnePerPhone;
        }
        if (Survey is null) ErrorMessage ??= "Không tìm thấy đợt khảo sát.";
        return Page();
    }

    private bool HasView() => Has("survey.view") || Has("survey.manage");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];
    private static DateTimeOffset? ToOffset(string value) => DateTimeOffset.TryParse(value + "+07:00", out var parsed) ? parsed : null;
    private static string LocalInput(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-ddTHH:mm");

    public sealed record SurveyHead(Guid Id, string Title, string Code, string Status, string? Summary, DateTimeOffset StartAt, DateTimeOffset EndAt, bool RequirePhone, string? Thanks, bool LimitOnePerPhone);
    public sealed record QuestionItem(Guid Id, string Text, string Type, bool Required, short Order);
    public sealed record OptionItem(Guid Id, Guid QuestionId, string Text, short? JumpToOrder);
    public sealed record Detail(SurveyHead? Survey, IReadOnlyList<QuestionItem>? Questions, IReadOnlyList<OptionItem>? Options);
    private sealed record DetailEnvelope(Detail? Item);
}
