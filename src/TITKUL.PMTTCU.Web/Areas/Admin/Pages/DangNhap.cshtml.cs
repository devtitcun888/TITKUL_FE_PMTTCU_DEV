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

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var token = await _api.LoginAsync(Username, Password);
        if (token is null)
        {
            ErrorMessage = "Đăng nhập không thành công.";
            return Page();
        }

        Response.Cookies.Append(AdminGateMiddleware.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            Path = "/"
        });
        return Redirect("/admin");
    }
}
