using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Pages;

namespace TITKUL.PMTTCU.Web.Pages;

public class ThongBaoChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public ThongBaoChiTietModel(BackendApiClient api) => _api = api;
    public NoticeItem? Item { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool ResourceNotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var result = await _api.GetPublicJsonResultAsync<ItemEnvelope>($"/api/v1/public/notices/{Uri.EscapeDataString(slug)}");
        var body = result.Value;
        Item = body?.Item;
        if (Item is null)
        {
            var notFound = result.IsNotFound || result.IsAvailable;
            ResourceNotFound = notFound;
            ErrorMessage = notFound ? "Không tìm thấy thông báo." : "Chưa thể tải thông báo. Vui lòng thử lại sau.";
            Response.StatusCode = notFound ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
        }
        else
        {
            ViewData["Title"] = Item.Title;
            ViewData["Description"] = CmsMetaText.FromHtml(Item.Html);
            ViewData["OpenGraphType"] = "article";
            ViewData["CanonicalPath"] = "/thong-bao/" + Uri.EscapeDataString(Item.Slug);
        }
        return Page();
    }

    public sealed record NoticeItem(string Title, string Slug, string Html, string Level, DateTimeOffset? VisibleFrom = null, DateTimeOffset? VisibleTo = null);
    private sealed record ItemEnvelope(NoticeItem? Item);
}
