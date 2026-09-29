using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class CoCauModel : PageModel
{
    private readonly BackendApiClient _api;
    public CoCauModel(BackendApiClient api) => _api = api;
    public string Html { get; private set; } = "";
    public bool Unavailable { get; private set; }

    public async Task OnGetAsync()
    {
        var result = await _api.GetPublicJsonResultAsync<GioiThieuModel.ConfigEnvelope>("/api/v1/public/site-config");
        Unavailable = !result.IsAvailable || result.Value is null;
        if (Unavailable) Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        var config = result.Value;
        Html = config?.Item is not null && config.Item.TryGetValue("page.co-cau", out var value) ? value : "";
    }
}
