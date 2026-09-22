using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TITKUL.PMTTCU.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string OrganizationName { get; private set; } = "";

    public void OnGet()
    {
        OrganizationName = _configuration["Pmttcu:OrganizationName"]
            ?? "Trung tâm cung ứng dịch vụ sự nghiệp công xã Tân Trụ";
    }
}
