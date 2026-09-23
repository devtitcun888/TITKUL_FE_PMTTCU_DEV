using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsBaiVietChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsBaiVietChiTietModel(BackendApiClient api) => _api = api;
    public Guid? PostId { get; private set; }
    public IReadOnlyList<CategoryItem> Categories { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? Status { get; private set; }
    public bool CanEdit { get; private set; }

    [BindProperty] public Guid CategoryId { get; set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Slug { get; set; }
    [BindProperty] public string? Summary { get; set; }
    [BindProperty] public string Html { get; set; } = "";
    [BindProperty] public string? SeoTitle { get; set; }
    [BindProperty] public string? SeoDescription { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid? id)
    {
        if (!Has("cms.create") && !Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var payload = new { categoryId = CategoryId, title = Title, slug = Slug, summary = Summary, html = Html, seoTitle = SeoTitle, seoDescription = SeoDescription };
        var response = id is Guid existing
            ? await _api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/posts/" + existing, token, payload)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/posts", token, payload);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được bài. Kiểm tra chuyên mục và nội dung.";
            return await LoadAsync(id);
        }

        var saved = await response.Content.ReadFromJsonAsync<ItemEnvelope<Saved>>();
        return Redirect("/admin/cms/bai-viet/sua/" + saved!.Item!.Id);
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/posts/" + id + "/publish", token, new { });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không đăng được bài.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostUnpublishAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/posts/" + id + "/unpublish", token, new { });
        return await LoadAsync(id);
    }

    private async Task<IActionResult> LoadAsync(Guid? id)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create") || Has("cms.update");
        var cats = await _api.GetJsonAsync<ListEnvelope<CategoryItem>>("/api/v1/admin/categories", token);
        Categories = cats?.Items ?? [];
        if (id is Guid existing)
        {
            PostId = existing;
            var body = await _api.GetJsonAsync<ItemEnvelope<PostDetail>>("/api/v1/admin/posts/" + existing, token);
            if (body?.Item is { } item)
            {
                CategoryId = item.CategoryId;
                Title = item.Title;
                Slug = item.Slug;
                Summary = item.Summary;
                Html = item.Html;
                SeoTitle = item.SeoTitle;
                SeoDescription = item.SeoDescription;
                Status = item.Status;
            }
            else ErrorMessage ??= "Không tìm thấy bài viết.";
        }

        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record CategoryItem(Guid Id, string Name);
    public sealed record PostDetail(Guid Id, Guid CategoryId, string Title, string Slug, string? Summary, string Html, string Status, string? SeoTitle, string? SeoDescription);
    public sealed record Saved(Guid Id);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
