using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class NguoiDungQuyenModel : PageModel
{
    private static readonly string[] ProtectedCodes = ["user.manage", "role.manage", "setting.manage", "backup.run"];
    private readonly BackendApiClient _api;

    public NguoiDungQuyenModel(BackendApiClient api) => _api = api;

    public Guid AccountId { get; private set; }
    public string Username { get; private set; } = "";
    public string FullName { get; private set; } = "";
    public IReadOnlyList<string> Roles { get; private set; } = [];
    public IReadOnlyList<string> RolePermissions { get; private set; } = [];
    public IReadOnlyList<string> DirectPermissions { get; private set; } = [];
    public IReadOnlyList<string> EffectivePermissions { get; private set; } = [];
    public IReadOnlyList<VaiTroModel.PermissionGroup> Groups { get; private set; } = [];
    public bool CanGrantProtected { get; private set; }
    public bool CanEdit { get; private set; }
    public string? Notice { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty] public string[]? Permissions { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id) => await LoadAsync(id);

    public async Task<IActionResult> OnPostSaveAsync(Guid id)
    {
        if (!HasUserManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/admin/users/{id}/permissions", token, new { permissions = Permissions ?? [] });
        Notice = response?.IsSuccessStatusCode == true
            ? "Đã lưu quyền trực tiếp. Người dùng cần đăng nhập lại để nhận quyền mới."
            : "Chưa lưu được quyền. Quyền quản trị cấp cao chỉ SUPER_ADMIN được thay đổi.";
        return await LoadAsync(id);
    }

    private async Task<IActionResult> LoadAsync(Guid id)
    {
        if (!HasUserManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        AccountId = id;
        CanGrantProtected = (HttpContext.Items["StaffProfile"] as StaffProfile)?.Roles?.Contains("SUPER_ADMIN") == true;
        CanEdit = true;

        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/admin/users/{id}/permissions", token);
        if (response?.IsSuccessStatusCode != true)
        {
            ErrorMessage = "Không tải được thông tin phân quyền tài khoản.";
            return Page();
        }
        var userBody = await response.Content.ReadFromJsonAsync<UserPermissionEnvelope>(Options());
        if (userBody?.User is null)
        {
            ErrorMessage = "Không tìm thấy tài khoản.";
            return Page();
        }
        Username = userBody.User.Username;
        FullName = userBody.User.FullName;
        Roles = userBody.Roles ?? [];
        RolePermissions = userBody.RolePermissions ?? [];
        DirectPermissions = userBody.DirectPermissions ?? [];
        EffectivePermissions = userBody.EffectivePermissions ?? [];

        using var catalogResponse = await SendAsync(HttpMethod.Get, "/api/v1/admin/permissions", token);
        if (catalogResponse?.IsSuccessStatusCode != true)
        {
            ErrorMessage = "Không tải được danh mục quyền.";
            return Page();
        }
        var catalog = await catalogResponse.Content.ReadFromJsonAsync<PermissionList>(Options());
        Groups = (catalog?.Items ?? []).GroupBy(item => item.Module)
            .Select(group => new VaiTroModel.PermissionGroup(group.Key, group.ToArray())).ToArray();
        if (!CanGrantProtected && Roles.Contains("SUPER_ADMIN", StringComparer.Ordinal)) CanEdit = false;
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

    private bool HasUserManage() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("user.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];
    private static System.Text.Json.JsonSerializerOptions Options() => new() { PropertyNameCaseInsensitive = true };

    public IReadOnlyList<string> DisabledPermissions => CanGrantProtected ? [] : ProtectedCodes;
    private sealed record UserPermissionEnvelope(AccountRow? User, IReadOnlyList<string>? Roles, IReadOnlyList<string>? RolePermissions, IReadOnlyList<string>? DirectPermissions, IReadOnlyList<string>? EffectivePermissions);
    private sealed record AccountRow(Guid Id, string Username, string FullName, string Status, IReadOnlyList<string> Roles);
    private sealed record PermissionList(IReadOnlyList<VaiTroModel.PermissionRow>? Items);
}
