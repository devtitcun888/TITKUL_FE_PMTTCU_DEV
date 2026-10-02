using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class TinTucChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public TinTucChiTietModel(BackendApiClient api) => _api = api;
    public PostItem? Item { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool ResourceNotFound { get; private set; }
    public string ReturnPath => Item?.CategoryKind switch { "HUONG_DAN_SO" => "/binh-dan-hoc-vu-so", "CHUONG_TRINH_HOC" => "/khoa-hoc-nghe", _ => "/tin-tuc" };
    public string ReturnLabel => Item?.CategoryKind switch { "HUONG_DAN_SO" => "Về Bình dân học vụ số", "CHUONG_TRINH_HOC" => "Về khóa học nghề", _ => "Về danh sách tin" };

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var result = await _api.GetPublicJsonResultAsync<ItemEnvelope>($"/api/v1/public/posts/{Uri.EscapeDataString(slug)}");
        var body = result.Value;
        Item = body?.Item;
        if (Item is null)
        {
            var notFound = result.IsNotFound || result.IsAvailable;
            ResourceNotFound = notFound;
            ErrorMessage = notFound ? "Không tìm thấy bài viết." : "Chưa thể tải bài viết. Vui lòng thử lại sau.";
            Response.StatusCode = notFound ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
        }
        else
        {
            if (Item.CategoryKind is "HUONG_DAN_SO" or "CHUONG_TRINH_HOC")
                return Redirect(ArticlePath(Item.CategoryKind) + Uri.EscapeDataString(Item.Slug));
            ViewData["Title"] = Item.SeoTitle ?? Item.Title;
            ViewData["Description"] = Item.SeoDescription ?? Item.Summary;
            ViewData["OpenGraphType"] = "article";
            ViewData["Image"] = Item.CoverUrl;
            ViewData["CanonicalPath"] = ArticlePath(Item.CategoryKind) + Uri.EscapeDataString(Item.Slug);
        }

        return Page();
    }

    private static string ArticlePath(string? kind) => kind switch { "HUONG_DAN_SO" => "/binh-dan-hoc-vu-so/bai-viet/", "CHUONG_TRINH_HOC" => "/khoa-hoc-nghe/bai-viet/", _ => "/tin-tuc/" };
    public sealed record PostItem(string Title, string Slug, string? Summary, string Html, DateTimeOffset? PublishedAt, string CategoryName, string? SeoTitle, string? SeoDescription, string? CoverUrl, string CategoryKind = "TIN_TUC");
    private sealed record ItemEnvelope(PostItem? Item);
}
