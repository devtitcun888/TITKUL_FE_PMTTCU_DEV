using System.Net.Http.Json;
using System.Text.Json;
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
    public bool ResourceNotFound { get; private set; }
    [BindProperty] public string? Phone { get; set; }
    [BindProperty] public string? Website { get; set; }
    [BindProperty] public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
    [TempData] public string? SubmittedThanks { get; set; }
    public Dictionary<Guid, SubmittedAnswer> SubmittedAnswers { get; } = [];

    public async Task OnGetAsync(string ma)
    {
        PublicPrivatePageHeaders.Apply(Response);
        var result = await _api.GetPublicJsonResultAsync<Envelope>("/api/v1/public/surveys/" + Uri.EscapeDataString(ma));
        var body = result.Value;
        Item = body?.Item;
        if (Item is null)
        {
            ResourceNotFound = result.IsNotFound || result.IsAvailable;
            ErrorMessage = ResourceNotFound
                ? "Không tìm thấy khảo sát hoặc đợt khảo sát không còn nhận phản hồi."
                : "Chưa thể tải khảo sát. Vui lòng thử lại sau.";
            Response.StatusCode = ResourceNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status503ServiceUnavailable;
            ViewData["Title"] = "Khảo sát";
        }
        else
        {
            SetPageMetadata(Item);
        }
    }

    public async Task<IActionResult> OnPostAsync(string ma)
    {
        PublicPrivatePageHeaders.Apply(Response);
        var currentResult = await _api.GetPublicJsonResultAsync<Envelope>("/api/v1/public/surveys/" + Uri.EscapeDataString(ma));
        var current = currentResult.Value;
        if (current?.Item is null)
        {
            ResourceNotFound = currentResult.IsNotFound || currentResult.IsAvailable;
            ErrorMessage = ResourceNotFound
                ? "Không tìm thấy khảo sát hoặc đợt khảo sát không còn nhận phản hồi."
                : "Chưa thể tải khảo sát. Vui lòng thử lại sau.";
            Response.StatusCode = ResourceNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status503ServiceUnavailable;
            return Page();
        }

        var questions = current.Item.Questions;
        var questionIndexByOrder = questions
            .Select((question, index) => (question.Order, index))
            .ToDictionary(item => item.Order, item => item.index);
        var visibleQuestionIds = new HashSet<Guid>();
        var questionIndex = 0;
        while (questionIndex < questions.Count)
        {
            var question = questions[questionIndex];
            if (!visibleQuestionIds.Add(question.Id)) break;

            if (question.Type == "SINGLE")
            {
                var selectedId = Request.Form["q-" + question.Id].ToString();
                var selected = current.Item.Options.FirstOrDefault(option =>
                    option.QuestionId == question.Id &&
                    string.Equals(option.Id.ToString(), selectedId, StringComparison.OrdinalIgnoreCase));
                if (selected?.JumpToOrder is short target && questionIndexByOrder.TryGetValue(target, out var nextIndex))
                {
                    questionIndex = nextIndex;
                    continue;
                }
            }

            questionIndex++;
        }

        var answers = new List<object>();
        foreach (var question in questions.Where(question => visibleQuestionIds.Contains(question.Id)))
        {
            var inputName = question.Type switch
            {
                "SINGLE" or "MULTIPLE" => "q-" + question.Id,
                "TEXT" => "t-" + question.Id,
                _ => "s-" + question.Id
            };
            if (!Request.Form.ContainsKey(inputName)) continue;
            if (question.Type == "SINGLE")
            {
                var value = Request.Form["q-" + question.Id].ToString();
                SubmittedAnswers[question.Id] = new(value, [], null, null);
                answers.Add(new { questionId = question.Id, optionId = Guid.TryParse(value, out var id) ? id : (Guid?)null });
            }
            else if (question.Type == "MULTIPLE")
            {
                var ids = Request.Form["q-" + question.Id].Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToArray();
                SubmittedAnswers[question.Id] = new(null, Request.Form["q-" + question.Id].ToArray(), null, null);
                answers.Add(new { questionId = question.Id, optionIds = ids });
            }
            else if (question.Type == "TEXT")
            {
                var value = Request.Form["t-" + question.Id].ToString();
                SubmittedAnswers[question.Id] = new(null, [], value, null);
                answers.Add(new { questionId = question.Id, text = value });
            }
            else
            {
                var raw = Request.Form["s-" + question.Id].ToString();
                SubmittedAnswers[question.Id] = new(null, [], null, raw);
                answers.Add(new { questionId = question.Id, scale = decimal.TryParse(raw, out var number) ? number : (decimal?)null });
            }
        }

        var key = IdempotencyKey;
        if (key.Length < 16) IdempotencyKey = key = Guid.NewGuid().ToString("N");
        using var response = await _api.PostPublicJsonAsync("/api/v1/public/surveys/" + Uri.EscapeDataString(ma) + "/responses", new { answers, phone = Phone, website = Website }, key);
        if (response is null || !response.IsSuccessStatusCode)
        {
            Item = current.Item;
            SetPageMetadata(current.Item);
            ApiError? error = null;
            try
            {
                if (response is not null) error = await response.Content.ReadFromJsonAsync<ApiError>();
            }
            catch (JsonException)
            {
                // Keep a safe generic message when the API error payload is malformed.
            }
            ErrorMessage = error?.Code switch
            {
                "SURVEY_PHONE_USED" => "Số điện thoại này đã gửi phản hồi cho khảo sát. Mỗi số chỉ được gửi một lần.",
                "SURVEY_RATE" => "Bạn gửi quá nhanh. Vui lòng đợi rồi thử lại.",
                "SURVEY_IDEMPOTENCY" => "Phiên gửi không khớp. Tải lại khảo sát rồi gửi lại.",
                "SURVEY_CLOSED" or "SURVEY_NOT_FOUND" => "Đợt khảo sát vừa đóng hoặc không còn nhận phiếu.",
                "SURVEY_PHONE" => "Vui lòng nhập số điện thoại hợp lệ.",
                "SURVEY_REQUIRED" => "Vui lòng trả lời các câu bắt buộc trong nhánh đang hiển thị.",
                "SURVEY_SCALE" => "Điểm đánh giá nằm ngoài khoảng cho phép. Kiểm tra lại câu hỏi và thử gửi lại.",
                "SURVEY_ANSWER" => "Một số câu trả lời không còn hợp lệ. Tải lại khảo sát rồi kiểm tra các câu đã chọn.",
                _ => "Chưa gửi được. Kiểm tra câu bắt buộc, câu trả lời và thử lại."
            };
            return Page();
        }
        try
        {
            var submitted = await response.Content.ReadFromJsonAsync<SubmittedEnvelope>();
            SubmittedThanks = submitted?.Item?.Thanks ?? "";
        }
        catch (JsonException)
        {
            SubmittedThanks = "";
        }
        return Redirect("/khao-sat/" + Uri.EscapeDataString(ma) + "/cam-on");
    }

    private void SetPageMetadata(SurveyBody survey)
    {
        ViewData["Title"] = survey.Title;
        ViewData["Description"] = survey.Summary;
        ViewData["CanonicalPath"] = "/khao-sat/" + Uri.EscapeDataString(survey.Code);
    }

    public sealed record OptionItem(Guid Id, Guid QuestionId, string Text, short? JumpToOrder);
    public sealed record QuestionItem(Guid Id, string Text, string Type, bool Required, short Order, short? ScaleMin, short? ScaleMax);
    public sealed record SubmittedAnswer(string? SingleOptionId, IReadOnlyList<string?> OptionIds, string? Text, string? Scale);
    public sealed record SurveyBody(string Code, string Title, string? Summary, bool RequirePhone, string? Thanks, IReadOnlyList<QuestionItem> Questions, IReadOnlyList<OptionItem> Options, bool LimitOnePerPhone = false);
    private sealed record Envelope(SurveyBody? Item);
    private sealed record SubmittedResponse(string Code, string? Thanks);
    private sealed record SubmittedEnvelope(SubmittedResponse? Item);
    private sealed record ApiError(string? Code, string? Message);
}
