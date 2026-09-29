using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class BaoMatModel : PageModel
{
    private readonly BackendApiClient _api;
    public BaoMatModel(BackendApiClient api) => _api = api;

    public bool MfaEnabled { get; private set; }
    public string? Secret { get; private set; }
    public string? ProvisioningUri { get; private set; }
    public string? Message { get; private set; }
    public string? Error { get; private set; }

    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    public async Task<IActionResult> OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostSetupAsync()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/auth/totp/setup", new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            Error = "Không tạo được khóa xác thực. Vui lòng thử lại.";
            return await LoadAsync();
        }
        var setup = await response.Content.ReadFromJsonAsync<TotpSetup>();
        Secret = setup?.Secret;
        ProvisioningUri = setup?.ProvisioningUri;
        Message = "Quét QR bằng ứng dụng Authenticator hoặc nhập khóa bí mật, sau đó xác nhận bằng mã 6 số.";
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostEnableAsync()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/auth/totp/enable", new { code = Code });
        if (response is null || !response.IsSuccessStatusCode) Error = "Mã xác thực không đúng. Mã có hiệu lực trong khoảng 30 giây.";
        else Message = "Đã bật xác thực hai bước cho tài khoản.";
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/auth/totp/disable", new { password = Password, code = Code });
        if (response is null || !response.IsSuccessStatusCode) Error = "Mật khẩu hoặc mã xác thực không đúng.";
        else Message = "Đã tắt xác thực hai bước.";
        return await LoadAsync();
    }

    private async Task<IActionResult> LoadAsync()
    {
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Redirect("/admin/dang-nhap");
        var status = await _api.GetJsonAsync<TotpStatus>("/api/v1/auth/totp/status", token);
        MfaEnabled = status?.Enabled ?? false;
        return Page();
    }

    private Task<HttpResponseMessage?> SendAsync(HttpMethod method, string path, object body)
    {
        var token = Request.Cookies[AdminGateMiddleware.CookieName];
        if (token is null) return Task.FromResult<HttpResponseMessage?>(null);
        return _api.SendJsonAsync(method, path, token, body);
    }

    private sealed record TotpStatus(bool Enabled);
    private sealed record TotpSetup(string? Secret, string? ProvisioningUri);
}
