using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class LopHocPublicModel : PageModel
{
    private readonly BackendApiClient _api;
    public LopHocPublicModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<Item> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        var body = await _api.GetPublicJsonAsync<ListEnvelope<Item>>("/api/v1/public/classes/open");
        if (body is null) ErrorMessage = "Không tải được danh sách lớp.";
        Items = body?.Items ?? [];
    }

    public sealed record Item(string Code, string Name, string ProgramName, DateOnly StartDate, DateOnly EndDate, int Remaining, bool CanRegister);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
