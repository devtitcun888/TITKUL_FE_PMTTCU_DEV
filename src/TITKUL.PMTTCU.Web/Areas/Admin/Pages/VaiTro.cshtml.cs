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
    public IReadOnlyList<PermissionGroup> Groups { get; private set; } = [];
    public bool CanManage { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Notice { get; private set; }

    [BindProperty] public string? Code { get; set; }
    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public string[]? Permissions { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!CanView()) return Redirect("/admin/khong-quyen");
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!CanManagePage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin/roles", token, new { code = Code, name = Name, description = Description, permissions = Permissions ?? [] });
        Notice = response?.IsSuccessStatusCode == true ? "Đã tạo vai trò." : "Chưa tạo được vai trò. Kiểm tra mã, tên và danh sách quyền.";
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid id, string name, string? description, string[]? permissions, bool active = false)
    {
        if (!CanManagePage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await SendAsync(HttpMethod.Put, "/api/v1/admin/roles/" + id, token,
            new { name, description, permissions = permissions ?? [], active });
        Notice = response?.IsSuccessStatusCode == true ? "Đã lưu cấu hình vai trò." : "Chưa lưu được. Một số quyền quản trị cấp cao chỉ SUPER_ADMIN được cấp.";
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        CanManage = CanManagePage();
        var token = Token();
        if (token is null) return;
        using var rolesResponse = await SendAsync(HttpMethod.Get, "/api/v1/admin/roles", token);
        if (rolesResponse?.IsSuccessStatusCode != true)
        {
            ErrorMessage = "Chưa tải được danh sách vai trò.";
            return;
        }
        var roles = await rolesResponse.Content.ReadFromJsonAsync<RoleList>(Options());
        Items = roles?.Items ?? [];

        using var permissionResponse = await SendAsync(HttpMethod.Get, "/api/v1/admin/permissions", token);
        if (permissionResponse?.IsSuccessStatusCode != true)
        {
            ErrorMessage = "Chưa tải được danh mục quyền.";
            return;
        }
        var catalog = await permissionResponse.Content.ReadFromJsonAsync<PermissionList>(Options());
        Groups = (catalog?.Items ?? []).GroupBy(item => item.Module)
            .Select(group => new PermissionGroup(group.Key, group.ToArray())).ToArray();
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

    private bool CanView() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Any(code => code is "role.view" or "role.manage") == true;
    private bool CanManagePage() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("role.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];
    private static System.Text.Json.JsonSerializerOptions Options() => new() { PropertyNameCaseInsensitive = true };

    public sealed record RoleRow(Guid Id, string Code, string Name, string? Description, IReadOnlyList<string> Permissions, bool IsSystem, bool Active);
    public sealed record PermissionRow(string Code, string Module, string Name, string Description);
    public sealed record PermissionGroup(string Name, IReadOnlyList<PermissionRow> Items);
    private sealed record RoleList(IReadOnlyList<RoleRow>? Items);
    private sealed record PermissionList(IReadOnlyList<PermissionRow>? Items);
}
