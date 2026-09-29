using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class BieuMauModel : PageModel
{
    private readonly BackendApiClient _api;
    public BieuMauModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<FormItem> Items { get; private set; } = [];
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? q, int page = 1)
    {
        page = Math.Max(1, page);
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var list = await _api.GetPublicJsonAsync<ListEnvelope<FormItem>>($"/api/v1/public/forms?page={page}&pageSize=20&q={Uri.EscapeDataString(Query ?? "")}");
        if (list is null) ErrorMessage = "Chưa thể tải danh sách biểu mẫu. Vui lòng thử lại sau.";
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? page;
        PageSize = list?.PageSize ?? 20;
        Total = list?.Total ?? Items.Count;
    }

    public async Task<IActionResult> OnGetTaiAsync(Guid id)
    {
        var file = await _api.GetFileResultAsync("/api/v1/public/files/forms/" + id);
        if (!file.IsSuccess) return file.IsNotFound ? NotFound() : StatusCode(503);
        return File(file.Bytes!, file.ContentType ?? "application/octet-stream", file.FileName ?? "bieu-mau");
    }

    public sealed record FormItem(Guid Id, string? Code, string Title, string? Summary, string FileName, DateTimeOffset? PublishedAt = null, DateOnly? EffectiveFrom = null, DateOnly? ExpiresOn = null);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
