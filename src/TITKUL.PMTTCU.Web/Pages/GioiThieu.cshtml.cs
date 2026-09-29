using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class GioiThieuModel : PageModel
{
    private readonly BackendApiClient _api;
    public GioiThieuModel(BackendApiClient api) => _api = api;
    public string Html { get; private set; } = "";
    public bool Unavailable { get; private set; }

    public async Task OnGetAsync()
    {
        var result = await _api.GetPublicJsonResultAsync<ConfigEnvelope>("/api/v1/public/site-config");
        Unavailable = !result.IsAvailable || result.Value is null;
        if (Unavailable) Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        Html = Value(result.Value, "page.gioi-thieu");
    }

    protected static string Value(ConfigEnvelope? config, string key) =>
        config?.Item is not null && config.Item.TryGetValue(key, out var value) ? value : "";

    public sealed record ConfigEnvelope(Dictionary<string, string>? Item);
}
