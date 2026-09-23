using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsAlbumChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsAlbumChiTietModel(BackendApiClient api) => _api = api;
    public AlbumItem? Item { get; private set; }
    public IReadOnlyList<MediaItem> Media { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanEdit { get; private set; }
    [BindProperty] public string? MediaTitle { get; set; }
    [BindProperty] public string? Alt { get; set; }
    [BindProperty] public string? VideoUrl { get; set; }
    [BindProperty] public IFormFile? Upload { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostFileAsync(Guid id)
    {
        if (!Has("cms.create")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (Upload is null)
        {
            ErrorMessage = "Chọn tệp.";
            return await LoadAsync(id);
        }

        using var content = new MultipartFormDataContent();
        if (!string.IsNullOrWhiteSpace(MediaTitle)) content.Add(new StringContent(MediaTitle), "title");
        if (!string.IsNullOrWhiteSpace(Alt)) content.Add(new StringContent(Alt), "alt");
        await using var stream = Upload.OpenReadStream();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(Upload.ContentType) ? "application/octet-stream" : Upload.ContentType);
        content.Add(file, "file", Upload.FileName);
        var response = await _api.PostMultipartAsync("/api/v1/admin/albums/" + id + "/files", token, content);
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không thêm được tệp. Ảnh cần mô tả alt. Học liệu chỉ nhận PDF/Word/Excel.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostLinkAsync(Guid id)
    {
        if (!Has("cms.create")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/albums/" + id + "/links", token, new { title = MediaTitle, url = VideoUrl });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Chỉ nhận link https YouTube hoặc Facebook.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/albums/" + id + "/publish", token, new { });
        return Redirect("/admin/cms/album/" + id);
    }

    private async Task<IActionResult> LoadAsync(Guid id)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        var body = await _api.GetJsonAsync<DetailEnvelope>("/api/v1/admin/albums/" + id, token);
        Item = body?.Item;
        Media = body?.Media ?? [];
        if (Item is null) ErrorMessage ??= "Không tìm thấy album.";
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record AlbumItem(Guid Id, string Title, string Slug, string Kind, string Status);
    public sealed record MediaItem(Guid Id, string Kind, string? Title, string? AltText, string? ExternalUrl);
    private sealed record DetailEnvelope(AlbumItem? Item, IReadOnlyList<MediaItem>? Media);
}
