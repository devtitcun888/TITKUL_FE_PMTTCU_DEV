using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class DangNhapModel : PageModel
{
    private readonly BackendApiClient _api;

    public DangNhapModel(BackendApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public string Username { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    [BindProperty]
    public string OtpCode { get; set; } = "";

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _api.LoginAsync(Username, Password, string.IsNullOrWhiteSpace(OtpCode) ? null : OtpCode);
        if (result.Token is null)
        {
            ErrorMessage = result.ErrorCode switch
            {
                "AUTH_OTP_REQUIRED" => "Tài khoản đã bật xác thực hai bước. Nhập mã 6 số từ ứng dụng xác thực.",
                "AUTH_OTP_INVALID" => "Mã xác thực không đúng hoặc đã hết hạn.",
                "AUTH_LOCKED" => "Tài khoản tạm khóa do nhập sai nhiều lần.",
                _ => "Đăng nhập không thành công. Kiểm tra tài khoản và mật khẩu."
            };
            return Page();
        }

        Response.Cookies.Append(AdminGateMiddleware.CookieName, result.Token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            Path = "/"
        });
        return Redirect("/admin");
    }
}
