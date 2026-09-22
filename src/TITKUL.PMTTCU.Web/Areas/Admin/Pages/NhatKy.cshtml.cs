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

    public async Task<IActionResult> OnGetAsync()
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _api.HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không tải được nhật ký.";
            return Page();
        }

        var body = await response.Content.ReadFromJsonAsync<AuditList>();
        Rows = body?.Items ?? [];
        return Page();
    }

    public sealed record AuditRow(DateTimeOffset OccurredAt, string Username, string Action, string Module, string TraceId);

    private sealed record AuditList(IReadOnlyList<AuditRow>? Items);
}
