using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class NguoiDungModel : PageModel
{
    private readonly BackendApiClient _api;
    public NguoiDungModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<AccountRow> Items { get; private set; } = [];
    public IReadOnlyList<RoleOption> RolesAvailable { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? Notice { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    [BindProperty] public string? Username { get; set; }
    [BindProperty] public string? FullName { get; set; }
    [BindProperty] public string? Password { get; set; }
    [BindProperty] public string[]? AssignedRoles { get; set; }

    public async Task<IActionResult> OnGetAsync(int page = 1) => await LoadAsync(page);

    public async Task<IActionResult> OnPostAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin/users", token,
            new { username = Username, fullName = FullName, password = Password, roles = AssignedRoles ?? [] });
        Notice = response?.IsSuccessStatusCode == true ? "Đã tạo tài khoản." : "Chưa tạo được. Kiểm tra tên, mật khẩu và vai trò.";
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostKhoaAsync(Guid id, string fullName)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await SendAsync(HttpMethod.Put, "/api/v1/admin/users/" + id, token, new { fullName, status = "LOCKED" });
        Notice = response?.IsSuccessStatusCode == true ? "Đã khóa tài khoản." : "Không khóa được tài khoản này.";
        return await LoadAsync();
    }

    private async Task<IActionResult> LoadAsync(int page = 1)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CurrentPage = Math.Max(1, page);
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/admin/users?page={CurrentPage}&pageSize={PageSize}", token);
        if (response?.IsSuccessStatusCode != true)
        {
            ErrorMessage = "Chưa tải được danh sách tài khoản.";
            return Page();
        }
        var body = await response.Content.ReadFromJsonAsync<AccountList>(Options());
        Items = body?.Items ?? [];
        CurrentPage = body?.Page ?? CurrentPage;
        PageSize = body?.PageSize ?? PageSize;
        Total = body?.Total ?? Items.Count;

        using var rolesResponse = await SendAsync(HttpMethod.Get, "/api/v1/admin/roles", token);
        if (rolesResponse?.IsSuccessStatusCode == true)
        {
            var roleBody = await rolesResponse.Content.ReadFromJsonAsync<RoleList>(Options());
            RolesAvailable = (roleBody?.Items ?? []).Where(role => role.Active).Select(role => new RoleOption(role.Code, role.Name, role.IsSystem)).ToArray();
        }
        return Page();
    }

    private async Task<HttpResponseMessage?> SendAsync(HttpMethod method, string path, string token, object? body = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await _api.HttpClient.SendAsync(request);
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
    }

    private bool CanManage() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("user.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];
    private static System.Text.Json.JsonSerializerOptions Options() => new() { PropertyNameCaseInsensitive = true };

    public sealed record AccountRow(Guid Id, string Username, string FullName, string Status, IReadOnlyList<string> Roles, IReadOnlyList<string>? RolePermissions, IReadOnlyList<string>? DirectPermissions);
    public sealed record RoleOption(string Code, string Name, bool IsSystem);
    private sealed record AccountList(IReadOnlyList<AccountRow>? Items, int? Page, int? PageSize, int? Total);
    private sealed record RoleList(IReadOnlyList<RoleDefinitionRow>? Items);
    private sealed record RoleDefinitionRow(string Code, string Name, bool IsSystem, bool Active);
}
