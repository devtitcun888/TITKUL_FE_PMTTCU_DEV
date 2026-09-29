using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class LopHocPublicModel : PageModel
{
    private readonly BackendApiClient _api;
    public LopHocPublicModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<Item> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? Query { get; private set; }

    public async Task OnGetAsync(string? q)
    {
        Query = q?.Trim();
        var path = "/api/v1/public/classes/open?limit=100";
        if (!string.IsNullOrWhiteSpace(Query)) path += "&q=" + Uri.EscapeDataString(Query);
        var body = await _api.GetPublicJsonAsync<ListEnvelope<Item>>(path);
        if (body is null) ErrorMessage = "Không tải được danh sách lớp.";
        Items = body?.Items ?? [];
    }

    public sealed record Item(string Code, string Name, string ProgramName, DateOnly StartDate, DateOnly EndDate, int Remaining, bool CanRegister, string? ClosedReason, DateTimeOffset? RegistrationClosesAt = null);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
