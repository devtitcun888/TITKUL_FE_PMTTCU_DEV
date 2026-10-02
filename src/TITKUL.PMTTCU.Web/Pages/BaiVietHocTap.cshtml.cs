using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class BaiVietHocTapModel : PageModel
{
    private readonly BackendApiClient _api;
    public BaiVietHocTapModel(BackendApiClient api) => _api = api;

    public PostItem? Item { get; private set; }
    public string Section { get; private set; } = "";
    public string BackUrl => Section == "binh-dan-hoc-vu-so" ? "/binh-dan-hoc-vu-so" : "/khoa-hoc-nghe";
    public string BackLabel => Section == "binh-dan-hoc-vu-so" ? "Về Bình dân học vụ số" : "Về các khóa học nghề";

    public async Task<IActionResult> OnGetAsync(string section, string slug)
    {
        if (section is not "binh-dan-hoc-vu-so" and not "khoa-hoc-nghe") return NotFound();
        Section = section;
        var result = await _api.GetPublicJsonAsync<ItemEnvelope>("/api/v1/public/posts/" + Uri.EscapeDataString(slug));
        Item = result?.Item;
        if (Item is null) return NotFound();
        var expectedKind = section == "binh-dan-hoc-vu-so" ? "HUONG_DAN_SO" : "CHUONG_TRINH_HOC";
        if (Item.CategoryKind != expectedKind) return NotFound();
        ViewData["Title"] = Item.SeoTitle ?? Item.Title;
        ViewData["Description"] = Item.SeoDescription ?? Item.Summary;
        ViewData["OpenGraphType"] = "article";
        ViewData["Image"] = Item.CoverUrl;
        ViewData["CanonicalPath"] = "/" + section + "/bai-viet/" + Uri.EscapeDataString(Item.Slug);
        return Page();
    }

    public sealed record PostItem(string Title, string Slug, string? Summary, string Html, DateTimeOffset? PublishedAt, string CategoryName, string? SeoTitle, string? SeoDescription, string? CoverUrl, string CategoryKind);
    private sealed record ItemEnvelope(PostItem? Item);
}
