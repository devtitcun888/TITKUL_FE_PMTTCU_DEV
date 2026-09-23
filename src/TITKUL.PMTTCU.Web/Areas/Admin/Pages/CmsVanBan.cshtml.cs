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

    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Symbol { get; set; }
    [BindProperty] public string? Field { get; set; }
    [BindProperty] public IFormFile? Upload { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync();
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

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(Title), "title");
        if (!string.IsNullOrWhiteSpace(Symbol)) content.Add(new StringContent(Symbol), "symbol");
        if (!string.IsNullOrWhiteSpace(Field)) content.Add(new StringContent(Field), "field");
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

    private async Task<IActionResult> LoadAsync()
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        var list = await _api.GetJsonAsync<ListEnvelope<DocumentItem>>("/api/v1/admin/documents?pageSize=50", token);
        Items = list?.Items ?? [];
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record DocumentItem(Guid Id, string? Symbol, string Title, string? Field, string FileName, string Status);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
