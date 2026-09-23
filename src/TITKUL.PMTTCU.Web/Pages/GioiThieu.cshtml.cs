using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class GioiThieuModel : PageModel
{
    private readonly BackendApiClient _api;
    public GioiThieuModel(BackendApiClient api) => _api = api;
    public string Html { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var config = await _api.GetPublicJsonAsync<ConfigEnvelope>("/api/v1/public/site-config");
        Html = Value(config, "page.gioi-thieu");
    }

    protected static string Value(ConfigEnvelope? config, string key) =>
        config?.Item is not null && config.Item.TryGetValue(key, out var value) ? value : "";

    public sealed record ConfigEnvelope(Dictionary<string, string>? Item);
}
