using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class SuKienModel : PageModel
{
    private readonly BackendApiClient _api;
    public SuKienModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<EventItem> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var list = await _api.GetPublicJsonAsync<ListEnvelope<EventItem>>("/api/v1/public/events?pageSize=20&upcoming=true");
        Items = list?.Items ?? [];
    }

    public sealed record EventItem(string Title, string Slug, string? Summary, DateTimeOffset StartAt, DateTimeOffset EndAt, string? Location);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
