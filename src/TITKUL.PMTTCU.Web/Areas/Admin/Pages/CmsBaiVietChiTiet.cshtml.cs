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
    public DateTimeOffset? PublishedAt { get; private set; }
    public bool CanEdit { get; private set; }
    public bool Preview { get; private set; }
    public string? PreviewHtml { get; private set; }
    public string? CoverUrl { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    [BindProperty(SupportsGet = true)] public string KindFilter { get; set; } = "TIN_TUC";
    public string KindTitle => KindFilter switch { "HUONG_DAN_SO" => "Bình dân học vụ số", "CHUONG_TRINH_HOC" => "Chương trình học", _ => "Tin tức - sự kiện" };
    public bool CanPublish { get; private set; }
    public string? SelectedCategoryName => Categories.FirstOrDefault(item => item.Id == CategoryId)?.Name;
    public string StatusLabel => string.IsNullOrWhiteSpace(Status) ? "Bài mới" : EducationUi.Status(Status);

    [BindProperty] public Guid CategoryId { get; set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Slug { get; set; }
    [BindProperty] public string? Summary { get; set; }
    [BindProperty] public string Html { get; set; } = "";
    [BindProperty] public string? SeoTitle { get; set; }
    [BindProperty] public string? SeoDescription { get; set; }
    [BindProperty] public string? CoverKey { get; set; }
    [BindProperty] public string? ThumbnailKey { get; set; }
    [BindProperty] public bool Pinned { get; set; }
    [BindProperty] public string? ScheduleAt { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, string? kind, bool preview = false)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        Preview = preview && id.HasValue;
        KindFilter = NormalizeKind(kind);
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid? id)
    {
        if (id.HasValue ? !Has("cms.update") : !Has("cms.create")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var payload = new { categoryId = CategoryId, title = Title, slug = Slug, summary = Summary, html = Html, coverKey = CoverKey, thumbnailKey = ThumbnailKey, pinned = Pinned, seoTitle = SeoTitle, seoDescription = SeoDescription };
        var response = id is Guid existing
            ? await _api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/posts/" + existing, token, payload)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/posts", token, payload);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được bài. Kiểm tra chuyên mục và nội dung.";
            return await LoadAsync(id, preserveForm: true);
        }

        var saved = await response.Content.ReadFromJsonAsync<ItemEnvelope<Saved>>();
        return Redirect("/admin/cms/bai-viet/sua/" + saved!.Item!.Id + "?kind=" + Uri.EscapeDataString(KindFilter));
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

    public async Task<IActionResult> OnPostScheduleAsync(Guid id)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (!DateTimeOffset.TryParse(ScheduleAt + "+07:00", out var publishAt) || publishAt <= DateTimeOffset.UtcNow)
        {
            ErrorMessage = "Chọn thời điểm xuất bản trong tương lai theo giờ Việt Nam.";
            return await LoadAsync(id, preserveForm: true);
        }
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/posts/" + id + "/schedule", token, new { publishAt });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không đặt được lịch xuất bản.";
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

    private async Task<IActionResult> LoadAsync(Guid? id, bool preserveForm = false)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = id.HasValue ? Has("cms.update") : Has("cms.create");
        CanPublish = Has("cms.publish");
        PostDetail? loaded = null;
        if (id is Guid existing)
        {
            PostId = existing;
            var body = await _api.GetJsonAsync<ItemEnvelope<PostDetail>>("/api/v1/admin/posts/" + existing, token);
            if (body?.Item is { } item)
            {
                loaded = item;
                KindFilter = NormalizeKind(item.CategoryKind);
                if (!preserveForm)
                {
                    CategoryId = item.CategoryId;
                    Title = item.Title;
                    Slug = item.Slug;
                    Summary = item.Summary;
                    Html = item.Html;
                    SeoTitle = item.SeoTitle;
                    SeoDescription = item.SeoDescription;
                    CoverKey = item.CoverKey;
                    ThumbnailKey = item.ThumbnailKey;
                    Pinned = item.Pinned;
                }
                PreviewHtml = body.PreviewHtml;
                CoverUrl = body.CoverUrl;
                ThumbnailUrl = body.ThumbnailUrl;
                Status = item.Status;
                PublishedAt = item.PublishedAt;
                Pinned = item.Pinned;
            }
            else ErrorMessage ??= "Không tìm thấy bài viết.";
        }

        var cats = await _api.GetJsonAsync<ListEnvelope<CategoryItem>>("/api/v1/admin/categories?kind=" + Uri.EscapeDataString(KindFilter), token);
        Categories = cats?.Items ?? [];
        if (!id.HasValue && CategoryId == Guid.Empty && Categories.Count > 0) CategoryId = Categories[0].Id;
        if (id.HasValue && !preserveForm && loaded is not null) CategoryId = loaded.CategoryId;

        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create") || Has("cms.update") || Has("cms.publish") || Has("cms.delete");
    private static string NormalizeKind(string? kind) => kind is "HUONG_DAN_SO" or "CHUONG_TRINH_HOC" ? kind : "TIN_TUC";
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record CategoryItem(Guid Id, string Name);
    public sealed record PostDetail(Guid Id, Guid CategoryId, string Title, string Slug, string? Summary, string Html, string? CoverKey, string Status, bool Pinned, string? SeoTitle, string? SeoDescription, DateTimeOffset? PublishedAt, string? ThumbnailKey = null, string CategoryKind = "TIN_TUC");
    public sealed record Saved(Guid Id);
    private sealed record ItemEnvelope<T>(T? Item, string? PreviewHtml, string? CoverUrl, string? ThumbnailUrl);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
