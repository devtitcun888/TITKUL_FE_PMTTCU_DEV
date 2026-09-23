using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class ThongBaoModel : PageModel
{
    private readonly BackendApiClient _api;
    public ThongBaoModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<NoticeItem> Urgent { get; private set; } = [];
    public IReadOnlyList<NoticeItem> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var list = await _api.GetPublicJsonAsync<NoticeList>("/api/v1/public/notices?pageSize=20");
        Urgent = list?.Urgent ?? [];
        Items = list?.Items ?? [];
    }

    public sealed record NoticeItem(string Title, string Slug, string Level);
    private sealed record NoticeList(IReadOnlyList<NoticeItem>? Items, IReadOnlyList<NoticeItem>? Urgent);
}
