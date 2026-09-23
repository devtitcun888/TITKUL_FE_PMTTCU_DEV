using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class LopHocMaModel : PageModel
{
    private readonly BackendApiClient _api;
    public LopHocMaModel(BackendApiClient api) => _api = api;
    public PublicClass? Item { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string ma)
    {
        var body = await _api.GetPublicJsonAsync<ItemEnvelope<PublicClass>>($"/api/v1/public/classes/{ma}");
        Item = body?.Item;
        if (Item is null) ErrorMessage = "Không tìm thấy lớp công khai.";
        return Page();
    }

    public sealed record PublicSession(string Title, DateTimeOffset StartAt, DateTimeOffset EndAt, string Mode, string Status);
    public sealed record PublicClass(string Code, string Name, string ProgramName, string? Description, DateOnly StartDate, DateOnly EndDate, int Capacity, int Remaining, string Status, bool CanRegister, string? ClosedReason, IReadOnlyList<PublicSession>? Sessions);
    private sealed record ItemEnvelope<T>(T? Item);
}
