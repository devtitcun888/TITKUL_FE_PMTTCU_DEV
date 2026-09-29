using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsAlbumModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsAlbumModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<AlbumItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanEdit { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? FilterKind { get; private set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string Kind { get; set; } = "IMAGE";

    public async Task<IActionResult> OnGetAsync(string? kind, int page = 1)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(page, kind);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Has("cms.create")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/albums", token, new { title = Title, kind = Kind });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không tạo được album.";
            return await LoadAsync();
        }

        var saved = await response.Content.ReadFromJsonAsync<ItemEnvelope<AlbumItem>>();
        return Redirect("/admin/cms/album/" + saved!.Item!.Id);
    }

    private async Task<IActionResult> LoadAsync(int page = 1, string? kind = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        FilterKind = kind is "IMAGE" or "VIDEO_LINK" or "HOC_LIEU" ? kind : null;
        CurrentPage = Math.Max(1, page);
        var path = $"/api/v1/admin/albums?page={CurrentPage}&pageSize={PageSize}";
        if (FilterKind is not null) path += "&kind=" + Uri.EscapeDataString(FilterKind);
        var list = await _api.GetJsonAsync<ListEnvelope<AlbumItem>>(path, token);
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? CurrentPage;
        PageSize = list?.PageSize ?? PageSize;
        Total = list?.Total ?? Items.Count;
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record AlbumItem(Guid Id, string Title, string Slug, string Kind, string Status);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
    private sealed record ItemEnvelope<T>(T? Item);
}
