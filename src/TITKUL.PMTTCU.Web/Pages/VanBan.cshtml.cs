using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class VanBanModel : PageModel
{
    private readonly BackendApiClient _api;
    public VanBanModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<DocumentItem> Items { get; private set; } = [];

    public async Task OnGetAsync(string? q, string? field)
    {
        var list = await _api.GetPublicJsonAsync<ListEnvelope<DocumentItem>>("/api/v1/public/documents?pageSize=20&q=" + Uri.EscapeDataString(q ?? "") + "&field=" + Uri.EscapeDataString(field ?? ""));
        Items = list?.Items ?? [];
    }

    public async Task<IActionResult> OnGetTaiAsync(Guid id)
    {
        var file = await _api.GetFileAsync("/api/v1/public/files/documents/" + id);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, file.ContentType ?? "application/octet-stream", file.FileName ?? "van-ban");
    }

    public sealed record DocumentItem(Guid Id, string? Symbol, string Title, string? Field, string FileName);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
