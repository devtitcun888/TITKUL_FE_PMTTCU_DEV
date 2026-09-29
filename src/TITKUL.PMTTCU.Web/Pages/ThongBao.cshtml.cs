using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class ThongBaoModel : PageModel
{
    private readonly BackendApiClient _api;
    public ThongBaoModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<NoticeItem> Urgent { get; private set; } = [];
    public IReadOnlyList<NoticeItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }

    public async Task OnGetAsync(int page = 1)
    {
        page = Math.Max(1, page);
        var list = await _api.GetPublicJsonAsync<NoticeList>($"/api/v1/public/notices?page={page}&pageSize=20");
        if (list is null) ErrorMessage = "Chưa thể tải danh sách thông báo. Vui lòng thử lại sau.";
        Urgent = list?.Urgent ?? [];
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? page;
        PageSize = list?.PageSize ?? 20;
        Total = list?.Total ?? Items.Count;
    }

    public sealed record NoticeItem(string Title, string Slug, string Level, DateTimeOffset? VisibleFrom = null, DateTimeOffset? VisibleTo = null);
    private sealed record NoticeList(IReadOnlyList<NoticeItem>? Items, IReadOnlyList<NoticeItem>? Urgent, int? Page, int? PageSize, int? Total);
}
