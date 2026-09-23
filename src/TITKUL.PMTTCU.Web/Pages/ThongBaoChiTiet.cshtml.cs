using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class ThongBaoChiTietModel : PageModel
{
    private readonly BackendApiClient _api;
    public ThongBaoChiTietModel(BackendApiClient api) => _api = api;
    public NoticeItem? Item { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var body = await _api.GetPublicJsonAsync<ItemEnvelope>($"/api/v1/public/notices/{slug}");
        Item = body?.Item;
        if (Item is null) ErrorMessage = "Không tìm thấy thông báo.";
        else ViewData["Title"] = Item.Title;
        return Page();
    }

    public sealed record NoticeItem(string Title, string Slug, string Html, string Level);
    private sealed record ItemEnvelope(NoticeItem? Item);
}
