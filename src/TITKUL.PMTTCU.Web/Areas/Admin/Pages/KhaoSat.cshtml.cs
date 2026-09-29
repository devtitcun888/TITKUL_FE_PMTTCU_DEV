using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class KhaoSatModel : PageModel
{
    private readonly BackendApiClient _api;
    public KhaoSatModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<SurveyItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    public bool CanViewResults { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public string? Status { get; private set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string StartAt { get; set; } = "";
    [BindProperty] public string EndAt { get; set; } = "";
    [BindProperty] public string? Thanks { get; set; }
    [BindProperty] public bool RequirePhone { get; set; }
    [BindProperty] public bool LimitOnePerPhone { get; set; }

    public async Task<IActionResult> OnGetAsync(string? q, string? status, int page = 1)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(page, q, status);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Has("survey.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/surveys", token, new
        {
            title = Title,
            startAt = ToOffset(StartAt),
            endAt = ToOffset(EndAt),
            thanks = Thanks,
            requirePhone = RequirePhone || LimitOnePerPhone,
            limitOnePerPhone = LimitOnePerPhone
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không tạo được. Giờ kết thúc phải sau giờ bắt đầu.";
            return await LoadAsync();
        }

        var saved = await response.Content.ReadFromJsonAsync<ItemEnvelope<SurveyItem>>();
        return Redirect("/admin/khao-sat/" + saved!.Item!.Id + "/thiet-ke");
    }

    private async Task<IActionResult> LoadAsync(int page = 1, string? q = null, string? status = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanManage = Has("survey.manage");
        CanViewResults = Has("survey.result.view");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        CurrentPage = Math.Max(1, page);
        var path = $"/api/v1/admin/surveys?page={CurrentPage}&pageSize={PageSize}";
        if (Query is not null) path += "&q=" + Uri.EscapeDataString(Query);
        if (Status is not null) path += "&status=" + Uri.EscapeDataString(Status);
        var list = await _api.GetJsonAsync<ListEnvelope<SurveyItem>>(path, token);
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? CurrentPage;
        PageSize = list?.PageSize ?? PageSize;
        Total = list?.Total ?? Items.Count;
        return Page();
    }

    private bool HasView() => Has("survey.view") || Has("survey.manage");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];
    private static DateTimeOffset? ToOffset(string value) => DateTimeOffset.TryParse(value + "+07:00", out var parsed) ? parsed : null;

    public sealed record SurveyItem(Guid Id, string Code, string Title, string Status);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
    private sealed record ItemEnvelope<T>(T? Item);
}
