using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class CauLacBoModel(BackendApiClient api) : PageModel
{
    public IReadOnlyList<PublicClubItem> Items { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        var envelope = await api.GetPublicJsonAsync<ListEnvelope<PublicClubItem>>("/api/v1/public/clubs");
        Items = envelope?.Items ?? [];
        if (envelope is null) Error = "Chưa tải được danh sách câu lạc bộ. Vui lòng thử lại sau.";
    }

    public sealed record Club(string Code, string Name, string? Description, string? Location, string? RegularSchedule);
    public sealed record ClubSession(string Title, DateTimeOffset StartAt, DateTimeOffset EndAt, string? Location, string? FacilitatorName);
    public sealed record PublicClass(string Code, string Name, DateOnly StartDate, DateOnly EndDate, string Status);
    public sealed record PublicClubItem(Club Club, IReadOnlyList<ClubSession> UpcomingSessions, IReadOnlyList<PublicClass> Classes);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
