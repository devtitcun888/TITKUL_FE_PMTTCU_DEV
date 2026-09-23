using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class KhaoSatCongKhaiModel : PageModel
{
    private readonly BackendApiClient _api;
    public KhaoSatCongKhaiModel(BackendApiClient api) => _api = api;
    public SurveyBody? Item { get; private set; }
    public string? ErrorMessage { get; private set; }
    [BindProperty] public string? Phone { get; set; }
    [BindProperty] public string? Website { get; set; }

    public async Task OnGetAsync(string ma)
    {
        var body = await _api.GetPublicJsonAsync<Envelope>("/api/v1/public/surveys/" + ma);
        Item = body?.Item;
        if (Item is null) ErrorMessage = "Đợt khảo sát đã đóng hoặc không còn nhận phiếu.";
    }

    public async Task<IActionResult> OnPostAsync(string ma)
    {
        var current = await _api.GetPublicJsonAsync<Envelope>("/api/v1/public/surveys/" + ma);
        if (current?.Item is null)
        {
            ErrorMessage = "Đợt khảo sát đã đóng hoặc không còn nhận phiếu.";
            return Page();
        }

        var answers = new List<object>();
        foreach (var question in current.Item.Questions)
        {
            if (question.Type == "SINGLE")
            {
                var value = Request.Form["q-" + question.Id].ToString();
                answers.Add(new { questionId = question.Id, optionId = Guid.TryParse(value, out var id) ? id : (Guid?)null });
            }
            else if (question.Type == "MULTIPLE")
            {
                var ids = Request.Form["q-" + question.Id].Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToArray();
                answers.Add(new { questionId = question.Id, optionIds = ids });
            }
            else if (question.Type == "TEXT")
            {
                answers.Add(new { questionId = question.Id, text = Request.Form["t-" + question.Id].ToString() });
            }
            else
            {
                var raw = Request.Form["s-" + question.Id].ToString();
                answers.Add(new { questionId = question.Id, scale = decimal.TryParse(raw, out var number) ? number : (decimal?)null });
            }
        }

        var key = Request.Form["IdempotencyKey"].ToString();
        if (key.Length < 16) key = Guid.NewGuid().ToString("N");
        var response = await _api.PostPublicJsonAsync("/api/v1/public/surveys/" + ma + "/responses", new { answers, phone = Phone, website = Website }, key);
        if (response is null || !response.IsSuccessStatusCode)
        {
            Item = current.Item;
            ErrorMessage = "Chưa gửi được. Kiểm tra câu bắt buộc và thang điểm.";
            return Page();
        }

        return Redirect("/khao-sat/" + ma + "/cam-on");
    }

    public sealed record OptionItem(Guid Id, Guid QuestionId, string Text);
    public sealed record QuestionItem(Guid Id, string Text, string Type, bool Required, short? ScaleMin, short? ScaleMax);
    public sealed record SurveyBody(string Code, string Title, string? Summary, bool RequirePhone, string? Thanks, IReadOnlyList<QuestionItem> Questions, IReadOnlyList<OptionItem> Options);
    private sealed record Envelope(SurveyBody? Item);
}
