using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class KhaoSatModel : PageModel
{
    private readonly BackendApiClient _api;

    public KhaoSatModel(BackendApiClient api) => _api = api;

    public string? Query { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<SurveyCard> Items { get; private set; } = [];
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }

    public async Task OnGetAsync(string? q, int page = 1)
    {
        Query = q?.Trim();
        if (Query?.Length > 80) Query = Query[..80];
        page = Math.Max(1, page);
        var path = $"/api/v1/public/surveys/open?page={page}&pageSize={PageSize}";
        if (!string.IsNullOrWhiteSpace(Query)) path += "&q=" + Uri.EscapeDataString(Query);
        var body = await _api.GetPublicJsonAsync<SurveyEnvelope>(path);
        if (body is null) ErrorMessage = "Chưa tải được danh sách khảo sát.";
        Items = body?.Items ?? [];
        CurrentPage = body?.Page ?? page;
        PageSize = body?.PageSize ?? PageSize;
        Total = body?.Total ?? Items.Count;
    }

    public sealed record SurveyCard(string Code, string Title, string? Summary, DateTimeOffset EndAt);
    private sealed record SurveyEnvelope(IReadOnlyList<SurveyCard>? Items, int? Page, int? PageSize, int? Total);
}
