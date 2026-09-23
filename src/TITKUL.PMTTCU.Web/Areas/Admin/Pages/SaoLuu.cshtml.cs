using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class SaoLuuModel : PageModel
{
    private readonly BackendApiClient _api;
    public SaoLuuModel(BackendApiClient api) => _api = api;
    public StatusBody? Item { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if ((HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("backup.view") != true)
        {
            return Redirect("/admin/khong-quyen");
        }

        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/backups/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _api.HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Chưa tải được trạng thái sao lưu.";
            return Page();
        }

        var body = await response.Content.ReadFromJsonAsync<Envelope>();
        Item = body?.Item;
        return Page();
    }

    public sealed record CopyRow(string Name, string Kind, long Bytes, string Sha256, DateTimeOffset CreatedAt);
    public sealed record StatusBody(string Mode, int RetentionPolicyDays, int DatabaseCopies, int MediaCopies, bool MeetsRetention, bool RunsFromApp, bool RestoresFromApp, string LiveCheck, string ReadyCheck, IReadOnlyList<CopyRow> Copies);
    private sealed record Envelope(StatusBody? Item);
}
