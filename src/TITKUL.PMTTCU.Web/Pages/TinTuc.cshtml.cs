using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class TinTucModel : PageModel
{
    private readonly BackendApiClient _api;
    public TinTucModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<PostItem> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var list = await _api.GetPublicJsonAsync<ListEnvelope<PostItem>>("/api/v1/public/posts?pageSize=20");
        Items = list?.Items ?? [];
    }

    public sealed record PostItem(string Title, string Slug, string? Summary, DateTimeOffset? PublishedAt, string CategoryName);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
