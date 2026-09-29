using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TITKUL.PMTTCU.Web.Pages;

public class KhaoSatCamOnModel : PageModel
{
    public string? Thanks { get; private set; }

    public void OnGet()
    {
        PublicPrivatePageHeaders.Apply(Response);
        Thanks = TempData["SubmittedThanks"] as string;
    }
}
