using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class NhatKyModel : PageModel
{
    private readonly BackendApiClient _api;

    public NhatKyModel(BackendApiClient api)
    {
        _api = api;
    }

    public IReadOnlyList<AuditRow> Rows { get; private set; } = [];

    public string? ErrorMessage { get; private set; }
    public string? ActionFilter { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? action, int page = 1)
    {
        var profile = HttpContext.Items["StaffProfile"] as StaffProfile;
        if (profile?.Permissions?.Contains("audit.view") != true)
        {
            return Redirect("/admin/khong-quyen");
        }

        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (string.IsNullOrWhiteSpace(token))
        {
            return Redirect("/admin/dang-nhap");
        }

        ActionFilter = string.IsNullOrWhiteSpace(action) ? null : action.Trim();
        CurrentPage = Math.Max(1, page);
        var query = $"?page={CurrentPage}&pageSize={PageSize}";
        if (ActionFilter is not null) query += "&action=" + Uri.EscapeDataString(ActionFilter);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit" + query);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _api.HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không tải được nhật ký.";
            return Page();
        }

        var body = await response.Content.ReadFromJsonAsync<AuditList>();
        Rows = body?.Items ?? [];
        CurrentPage = body?.Page ?? CurrentPage;
        PageSize = body?.PageSize ?? PageSize;
        Total = body?.Total ?? Rows.Count;
        return Page();
    }

    public async Task<IActionResult> OnGetXuatAsync(string? action)
    {
        var profile = HttpContext.Items["StaffProfile"] as StaffProfile;
        if (profile?.Permissions?.Contains("audit.view") != true) return Redirect("/admin/khong-quyen");
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (string.IsNullOrWhiteSpace(token)) return Redirect("/admin/dang-nhap");
        var path = "/api/v1/admin/audit/export";
        if (!string.IsNullOrWhiteSpace(action)) path += "?action=" + Uri.EscapeDataString(action.Trim());
        var file = await _api.GetFileAsync(path, token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, "text/csv; charset=utf-8", "nhat-ky-quan-tri.csv");
    }

    public sealed record AuditRow(DateTimeOffset OccurredAt, string Username, string Action, string Module, string TraceId, string? EntityType, Guid? EntityId);

    private sealed record AuditList(IReadOnlyList<AuditRow>? Items, int? Page, int? PageSize, int? Total);
}
