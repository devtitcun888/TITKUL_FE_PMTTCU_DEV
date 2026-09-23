using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class VaiTroModel : PageModel
{
    private readonly BackendApiClient _api;
    public VaiTroModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<RoleRow> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if ((HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("user.manage") != true)
        {
            return Redirect("/admin/khong-quyen");
        }

        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _api.HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Chưa tải được vai trò.";
            return Page();
        }

        var body = await response.Content.ReadFromJsonAsync<RoleList>();
        Items = body?.Items ?? [];
        return Page();
    }

    public sealed record RoleRow(string Code, bool ReadOnly);
    private sealed record RoleList(IReadOnlyList<RoleRow>? Items);
}
