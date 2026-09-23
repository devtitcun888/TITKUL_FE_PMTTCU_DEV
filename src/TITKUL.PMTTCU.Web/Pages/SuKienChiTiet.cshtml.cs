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

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var body = await _api.GetPublicJsonAsync<ItemEnvelope>($"/api/v1/public/events/{slug}");
        Item = body?.Item;
        if (Item is null) ErrorMessage = "Không tìm thấy sự kiện.";
        else ViewData["Title"] = Item.Title;
        return Page();
    }

    public sealed record EventItem(string Title, string Slug, string? Summary, string? Html, DateTimeOffset StartAt, DateTimeOffset EndAt, string? Location);
    private sealed record ItemEnvelope(EventItem? Item);
}
