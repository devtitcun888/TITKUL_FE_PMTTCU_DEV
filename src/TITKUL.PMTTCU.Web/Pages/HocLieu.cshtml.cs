using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class HocLieuModel : PageModel
{
    private readonly BackendApiClient _api;
    public HocLieuModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<AlbumItem> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var list = await _api.GetPublicJsonAsync<ListEnvelope<AlbumItem>>("/api/v1/public/albums?kind=HOC_LIEU&pageSize=20");
        Items = list?.Items ?? [];
    }

    public sealed record AlbumItem(string Title, string Slug, string? Summary);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
