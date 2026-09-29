using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class IndexModel : PageModel
{
    public static readonly string[] Modules = ["Cms", "Education", "Learners", "Surveys", "Reporting", "Identity", "Audit"];

    public IActionResult OnGet() => Redirect("/admin/tong-quan");
}
