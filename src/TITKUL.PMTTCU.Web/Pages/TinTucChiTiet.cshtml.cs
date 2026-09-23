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

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var body = await _api.GetPublicJsonAsync<ItemEnvelope>($"/api/v1/public/posts/{slug}");
        Item = body?.Item;
        if (Item is null) ErrorMessage = "Không tìm thấy bài viết.";
        else
        {
            ViewData["Title"] = Item.SeoTitle ?? Item.Title;
            ViewData["Description"] = Item.SeoDescription ?? Item.Summary;
        }

        return Page();
    }

    public sealed record PostItem(string Title, string Slug, string? Summary, string Html, DateTimeOffset? PublishedAt, string CategoryName, string? SeoTitle, string? SeoDescription);
    private sealed record ItemEnvelope(PostItem? Item);
}
