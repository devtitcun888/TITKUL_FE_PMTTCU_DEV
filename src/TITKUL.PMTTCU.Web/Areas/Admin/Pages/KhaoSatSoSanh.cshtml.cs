using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class KhaoSatSoSanhModel : PageModel
{
    private readonly BackendApiClient _api;
    public KhaoSatSoSanhModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<SurveyItem> Surveys { get; private set; } = [];
    public IReadOnlyList<Guid> SelectedIds { get; private set; } = [];
    public Comparison? Result { get; private set; }
    public string? Query { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; } = 50;
    public int Total { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));

    public async Task<IActionResult> OnGetAsync(string[]? ids, string? q, int page = 1, int? navigatePage = null, bool compare = false)
    {
        if (!Has("survey.result.view")) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");

        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        CurrentPage = Math.Max(1, navigatePage ?? page);
        SelectedIds = ParseIds(ids);

        var listPath = $"/api/v1/admin/surveys?page={CurrentPage}&pageSize={PageSize}";
        if (Query is not null) listPath += "&q=" + Uri.EscapeDataString(Query);
        var list = await _api.GetJsonAsync<SurveyListEnvelope>(listPath, token);
        Surveys = list?.Items ?? [];
        Total = list?.Total ?? Surveys.Count;
        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
            listPath = $"/api/v1/admin/surveys?page={CurrentPage}&pageSize={PageSize}";
            if (Query is not null) listPath += "&q=" + Uri.EscapeDataString(Query);
            list = await _api.GetJsonAsync<SurveyListEnvelope>(listPath, token);
            Surveys = list?.Items ?? [];
            Total = list?.Total ?? Surveys.Count;
        }

        if (navigatePage is not null) return Page();
        if (!compare) return Page();
        if (ids is null || ids.Length is < 2 or > 10 || ids.Any(id => !Guid.TryParse(id, out _)))
        {
            ErrorMessage = "Chọn từ 2 đến 10 khảo sát hợp lệ để so sánh.";
            return Page();
        }

        if (SelectedIds.Count != ids.Length)
        {
            ErrorMessage = "Danh sách có khảo sát bị chọn lặp.";
            return Page();
        }

        var comparePath = "/api/v1/admin/surveys/compare?ids=" + Uri.EscapeDataString(string.Join(',', SelectedIds));
        var response = await _api.GetJsonAsync<ComparisonEnvelope>(comparePath, token);
        Result = response?.Item;
        if (Result is null) ErrorMessage = "Không tải được kết quả so sánh. Vui lòng thử lại.";
        return Page();
    }

    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;

    private static IReadOnlyList<Guid> ParseIds(string[]? ids) => ids is null
        ? []
        : ids.Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(11)
            .ToArray();

    public sealed record SurveyItem(Guid Id, string Code, string Title, string Status);
    public sealed record Comparison(IReadOnlyList<ComparisonSurvey> Surveys, IReadOnlyList<ComparisonQuestion> Questions);
    public sealed record ComparisonSurvey(Guid Id, string Code, string Title, int Responses);
    public sealed record ComparisonQuestion(string Text, string Type, int MatchedSurveys, IReadOnlyList<ComparisonRow> Rows);
    public sealed record ComparisonRow(string Label, IReadOnlyList<int> Counts);
    private sealed record SurveyListEnvelope(IReadOnlyList<SurveyItem>? Items, int? Page, int? PageSize, int? Total);
    private sealed record ComparisonEnvelope(Comparison? Item);
}
