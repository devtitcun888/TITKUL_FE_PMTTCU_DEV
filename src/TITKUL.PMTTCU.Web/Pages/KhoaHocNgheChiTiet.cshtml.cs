using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class KhoaHocNgheChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public KhoaHocNgheChiTietModel(BackendApiClient api) => _api = api;

    public ProgramItem? Item { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var result = await _api.GetPublicJsonAsync<ItemEnvelope<ProgramItem>>("/api/v1/public/programs/" + Uri.EscapeDataString(slug));
        if (result?.Item is null) return NotFound();
        Item = result.Item;
        return Page();
    }

    public sealed record ProgramItem(Guid Id, string Code, string Name, string Slug, Guid CategoryId, string CategoryName, string? Summary, string? ContentHtml, string? CoverUrl, string? SeoTitle, string? SeoDescription, IReadOnlyList<OpenClassItem>? OpenClasses = null);
    public sealed record OpenClassItem(string Code, string Name, string ProgramName, DateOnly StartDate, DateOnly EndDate, int Capacity, int Remaining, string Status, bool CanRegister, string? ClosedReason);
    private sealed record ItemEnvelope<T>(T? Item);
}
