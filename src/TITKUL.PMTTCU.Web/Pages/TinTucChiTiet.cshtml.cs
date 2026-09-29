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
            ViewData["Title"] = Item.SeoTitle ?? Item.Title;
            ViewData["Description"] = Item.SeoDescription ?? Item.Summary;
            ViewData["OpenGraphType"] = "article";
            ViewData["Image"] = Item.CoverUrl;
            ViewData["CanonicalPath"] = "/tin-tuc/" + Uri.EscapeDataString(Item.Slug);
        }

        return Page();
    }

    public sealed record PostItem(string Title, string Slug, string? Summary, string Html, DateTimeOffset? PublishedAt, string CategoryName, string? SeoTitle, string? SeoDescription, string? CoverUrl);
    private sealed record ItemEnvelope(PostItem? Item);
}
