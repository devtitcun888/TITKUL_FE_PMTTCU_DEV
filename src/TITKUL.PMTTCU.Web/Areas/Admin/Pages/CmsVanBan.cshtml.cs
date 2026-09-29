using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsVanBanModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsVanBanModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<DocumentItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanEdit { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public string? FieldFilter { get; private set; }

    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Symbol { get; set; }
    [BindProperty] public string? Field { get; set; }
    [BindProperty] public string? Issuer { get; set; }
    [BindProperty] public DateOnly? IssuedOn { get; set; }
    [BindProperty] public DateOnly? EffectiveFrom { get; set; }
    [BindProperty] public DateOnly? ExpiresOn { get; set; }
    [BindProperty] public IFormFile? Upload { get; set; }

    public async Task<IActionResult> OnGetAsync(string? q, string? field, int page = 1)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(page, q, field);
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
            ErrorMessage = "Tệp văn bản không được vượt quá 20 MiB.";
            return await LoadAsync();
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(Title), "title");
        if (!string.IsNullOrWhiteSpace(Symbol)) content.Add(new StringContent(Symbol), "symbol");
        if (!string.IsNullOrWhiteSpace(Field)) content.Add(new StringContent(Field), "field");
        if (!string.IsNullOrWhiteSpace(Issuer)) content.Add(new StringContent(Issuer), "issuer");
        if (IssuedOn is DateOnly issuedOn) content.Add(new StringContent(issuedOn.ToString("yyyy-MM-dd")), "issuedOn");
        if (EffectiveFrom is DateOnly effectiveFrom) content.Add(new StringContent(effectiveFrom.ToString("yyyy-MM-dd")), "effectiveFrom");
        if (ExpiresOn is DateOnly expiresOn) content.Add(new StringContent(expiresOn.ToString("yyyy-MM-dd")), "expiresOn");
        await using var stream = Upload.OpenReadStream();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(Upload.ContentType) ? "application/octet-stream" : Upload.ContentType);
        content.Add(file, "file", Upload.FileName);
        var response = await _api.PostMultipartAsync("/api/v1/admin/documents", token, content);
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không lưu được văn bản. Kiểm tra loại file và dung lượng.";
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/documents/" + id + "/publish", token, new { });
        return Redirect("/admin/cms/van-ban");
    }

    private async Task<IActionResult> LoadAsync(int page = 1, string? q = null, string? field = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        FieldFilter = string.IsNullOrWhiteSpace(field) ? null : field.Trim();
        CurrentPage = Math.Max(1, page);
        var path = $"/api/v1/admin/documents?page={CurrentPage}&pageSize={PageSize}&q={Uri.EscapeDataString(Query ?? "")}&field={Uri.EscapeDataString(FieldFilter ?? "")}";
        var list = await _api.GetJsonAsync<ListEnvelope<DocumentItem>>(path, token);
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? CurrentPage;
        PageSize = list?.PageSize ?? PageSize;
        Total = list?.Total ?? Items.Count;
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record DocumentItem(Guid Id, string? Symbol, string Title, string? Issuer, DateOnly? IssuedOn, string? Field, string FileName, string Status, DateOnly? EffectiveFrom = null, DateOnly? ExpiresOn = null);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
