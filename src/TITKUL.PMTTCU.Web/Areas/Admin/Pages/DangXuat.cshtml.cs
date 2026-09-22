using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class DangXuatModel : PageModel
{
    public IActionResult OnGet()
    {
        Response.Cookies.Delete(AdminGateMiddleware.CookieName);
        return Redirect("/admin/dang-nhap");
    }
}
