using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class VanHoaModel : PageModel
{
    private readonly BackendApiClient _api;
    public VanHoaModel(BackendApiClient api) => _api = api;
    public string Html { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var config = await _api.GetPublicJsonAsync<GioiThieuModel.ConfigEnvelope>("/api/v1/public/site-config");
        Html = config?.Item is not null && config.Item.TryGetValue("page.van-hoa", out var value) ? value : "";
    }
}
