using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly BackendApiClient _api;

    public IndexModel(IConfiguration configuration, BackendApiClient api)
    {
        _configuration = configuration;
        _api = api;
    }

    public string OrganizationName { get; private set; } = "";
    public IReadOnlyList<NoticeItem> Urgent { get; private set; } = [];
    public IReadOnlyList<PostItem> Pinned { get; private set; } = [];
    public IReadOnlyList<PostItem> Latest { get; private set; } = [];
    public IReadOnlyList<EventItem> Upcoming { get; private set; } = [];
    public IReadOnlyList<MaterialItem> Materials { get; private set; } = [];

    public async Task OnGetAsync()
    {
        OrganizationName = _configuration["Pmttcu:OrganizationName"]
            ?? "Trung tâm cung ứng dịch vụ sự nghiệp công xã Tân Trụ";
        var home = await _api.GetPublicJsonAsync<ItemEnvelope>("/api/v1/public/home");
        if (home?.Item is { } item)
        {
            Urgent = item.Urgent ?? [];
            Pinned = item.Pinned ?? [];
            Latest = item.Latest ?? [];
            Upcoming = item.Upcoming ?? [];
            Materials = item.Materials ?? [];
        }
    }

    public sealed record NoticeItem(string Title, string Slug, string Level);
    public sealed record PostItem(string Title, string Slug, string? Summary, DateTimeOffset? PublishedAt);
    public sealed record EventItem(string Title, string Slug, DateTimeOffset StartAt, DateTimeOffset EndAt, string? Location);
    public sealed record MaterialItem(string Title, string Slug, string? Summary);
    private sealed record HomeBody(IReadOnlyList<NoticeItem>? Urgent, IReadOnlyList<PostItem>? Pinned, IReadOnlyList<PostItem>? Latest, IReadOnlyList<EventItem>? Upcoming, IReadOnlyList<MaterialItem>? Materials);
    private sealed record ItemEnvelope(HomeBody? Item);
}
