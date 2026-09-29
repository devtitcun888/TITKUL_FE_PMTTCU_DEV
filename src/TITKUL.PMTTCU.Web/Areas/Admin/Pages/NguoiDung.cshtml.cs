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
    public string? ErrorMessage { get; private set; }
    public string? Notice { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    [BindProperty] public string? Username { get; set; }
    [BindProperty] public string? FullName { get; set; }
    [BindProperty] public string? Password { get; set; }
    [BindProperty] public string? Role { get; set; }

    public async Task<IActionResult> OnGetAsync(int page = 1) => await LoadAsync(page);

    public async Task<IActionResult> OnPostAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { username = Username, fullName = FullName, password = Password, roles = new[] { Role } });
        using var response = await _api.HttpClient.SendAsync(request);
        Notice = response.IsSuccessStatusCode ? "Đã tạo tài khoản." : "Chưa tạo được. Kiểm tra tên, mật khẩu và vai trò.";
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostKhoaAsync(Guid id, string fullName)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/admin/users/" + id);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { fullName, status = "LOCKED" });
        using var response = await _api.HttpClient.SendAsync(request);
        Notice = response.IsSuccessStatusCode ? "Đã khóa tài khoản." : "Không khóa được tài khoản này.";
        return await LoadAsync();
    }

    private async Task<IActionResult> LoadAsync(int page = 1)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CurrentPage = Math.Max(1, page);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/users?page={CurrentPage}&pageSize={PageSize}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _api.HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Chưa tải được danh sách.";
            return Page();
        }

        var body = await response.Content.ReadFromJsonAsync<AccountList>();
        Items = body?.Items ?? [];
        CurrentPage = body?.Page ?? CurrentPage;
        PageSize = body?.PageSize ?? PageSize;
        Total = body?.Total ?? Items.Count;
        return Page();
    }

    private bool CanManage() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("user.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record AccountRow(Guid Id, string Username, string FullName, string Status, IReadOnlyList<string> Roles);
    private sealed record AccountList(IReadOnlyList<AccountRow>? Items, int? Page, int? PageSize, int? Total);
}
