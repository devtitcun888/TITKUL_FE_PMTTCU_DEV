using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class LopHocMaModel : PageModel
{
    private readonly BackendApiClient _api;
    public LopHocMaModel(BackendApiClient api) => _api = api;
    public PublicClass? Item { get; private set; }
    public JoinStatus? Join { get; private set; }
    public IReadOnlyList<PublicMaterial> Materials { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string ma)
    {
        var body = await _api.GetPublicJsonAsync<ItemEnvelope<PublicClass>>($"/api/v1/public/classes/{ma}");
        Item = body?.Item;
        if (Item is null) ErrorMessage = "Không tìm thấy lớp công khai.";
        var join = await _api.GetPublicJsonAsync<ItemEnvelope<JoinStatus>>($"/api/v1/public/classes/{ma}/join-status");
        Join = join?.Item;
        var materials = await _api.GetPublicJsonAsync<ListEnvelope<PublicMaterial>>($"/api/v1/public/classes/{ma}/materials");
        Materials = materials?.Items ?? [];
        return Page();
    }

    public async Task<IActionResult> OnGetHocLieuAsync(string ma, Guid id)
    {
        var file = await _api.GetFileAsync($"/api/v1/public/materials/{id}/file");
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, file.ContentType ?? "application/octet-stream", file.FileName ?? "hoc-lieu");
    }

    public sealed record PublicSession(string Title, DateTimeOffset StartAt, DateTimeOffset EndAt, string Mode, string Status);
    public sealed record PublicClass(string Code, string Name, string ProgramName, string? Description, DateOnly StartDate, DateOnly EndDate, int Capacity, int Remaining, string Status, bool CanRegister, string? ClosedReason, IReadOnlyList<PublicSession>? Sessions);
    public sealed record JoinStatus(bool CanJoin, string? Url, string? SessionTitle, DateTimeOffset? StartAt, DateTimeOffset? EndAt, string? Reason);
    public sealed record PublicMaterial(Guid Id, string Title, string? Description, string FileName, string MimeType, long Size);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
