using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsBieuMauModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsBieuMauModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<FormItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public bool CanEdit { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }

    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Code { get; set; }
    [BindProperty] public string? Summary { get; set; }
    [BindProperty] public DateOnly? EffectiveFrom { get; set; }
    [BindProperty] public DateOnly? ExpiresOn { get; set; }
    [BindProperty] public IFormFile? Upload { get; set; }

    public async Task<IActionResult> OnGetAsync(string? q, int page = 1)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(page, q);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Has("cms.create")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (Upload is null)
        {
            ErrorMessage = "Chọn file PDF, Word hoặc Excel.";
            return await LoadAsync();
        }
        if (Upload.Length > 20 * 1024 * 1024)
        {
            ErrorMessage = "Tệp biểu mẫu không được vượt quá 20 MiB.";
            return await LoadAsync();
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(Title), "title");
        if (!string.IsNullOrWhiteSpace(Code)) content.Add(new StringContent(Code), "code");
        if (!string.IsNullOrWhiteSpace(Summary)) content.Add(new StringContent(Summary), "summary");
        if (EffectiveFrom is DateOnly effectiveFrom) content.Add(new StringContent(effectiveFrom.ToString("yyyy-MM-dd")), "effectiveFrom");
        if (ExpiresOn is DateOnly expiresOn) content.Add(new StringContent(expiresOn.ToString("yyyy-MM-dd")), "expiresOn");
        await using var stream = Upload.OpenReadStream();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(Upload.ContentType) ? "application/octet-stream" : Upload.ContentType);
        content.Add(file, "file", Upload.FileName);
        var response = await _api.PostMultipartAsync("/api/v1/admin/forms", token, content);
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không lưu được biểu mẫu.";
        else
        {
            var created = await response.Content.ReadFromJsonAsync<ItemEnvelope<FormItem>>();
            SuccessMessage = created?.Item?.Code is { Length: > 0 } code
                ? $"Đã lưu biểu mẫu. Mã được hệ thống cấp: {code}."
                : "Đã lưu biểu mẫu. Mã được hệ thống cấp và có trong danh sách.";
        }
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/forms/" + id + "/publish", token, new { });
        return Redirect("/admin/cms/bieu-mau");
    }

    private async Task<IActionResult> LoadAsync(int page = 1, string? q = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        CurrentPage = Math.Max(1, page);
        var list = await _api.GetJsonAsync<ListEnvelope<FormItem>>($"/api/v1/admin/forms?page={CurrentPage}&pageSize={PageSize}&q={Uri.EscapeDataString(Query ?? "")}", token);
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? CurrentPage;
        PageSize = list?.PageSize ?? PageSize;
        Total = list?.Total ?? Items.Count;
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record FormItem(Guid Id, string? Code, string Title, string Status, DateTimeOffset? PublishedAt = null, DateOnly? EffectiveFrom = null, DateOnly? ExpiresOn = null);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
