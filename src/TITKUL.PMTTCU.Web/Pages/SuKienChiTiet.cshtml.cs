using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class SuKienChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public SuKienChiTietModel(BackendApiClient api) => _api = api;
    public EventItem? Item { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool ResourceNotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var result = await _api.GetPublicJsonResultAsync<ItemEnvelope>($"/api/v1/public/events/{Uri.EscapeDataString(slug)}");
        var body = result.Value;
        Item = body?.Item;
        if (Item is null)
        {
            var notFound = result.IsNotFound || result.IsAvailable;
            ResourceNotFound = notFound;
            ErrorMessage = notFound ? "Không tìm thấy sự kiện." : "Chưa thể tải sự kiện. Vui lòng thử lại sau.";
            Response.StatusCode = notFound ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
        }
        else
        {
            ViewData["Title"] = Item.Title;
            ViewData["Description"] = Item.Summary;
            ViewData["OpenGraphType"] = "article";
            ViewData["CanonicalPath"] = "/su-kien/" + Uri.EscapeDataString(Item.Slug);
        }
        return Page();
    }

    public sealed record EventItem(string Title, string Slug, string? Summary, string? Html, DateTimeOffset StartAt, DateTimeOffset EndAt, string? Location);
    private sealed record ItemEnvelope(EventItem? Item);
}
