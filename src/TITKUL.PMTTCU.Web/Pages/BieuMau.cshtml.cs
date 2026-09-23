using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class BieuMauModel : PageModel
{
    private readonly BackendApiClient _api;
    public BieuMauModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<FormItem> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var list = await _api.GetPublicJsonAsync<ListEnvelope<FormItem>>("/api/v1/public/forms?pageSize=20");
        Items = list?.Items ?? [];
    }

    public async Task<IActionResult> OnGetTaiAsync(Guid id)
    {
        var file = await _api.GetFileAsync("/api/v1/public/files/forms/" + id);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, file.ContentType ?? "application/octet-stream", file.FileName ?? "bieu-mau");
    }

    public sealed record FormItem(Guid Id, string? Code, string Title, string? Summary, string FileName);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
