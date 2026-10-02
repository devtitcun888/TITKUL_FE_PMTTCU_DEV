using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsAlbumModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsAlbumModel(BackendApiClient api) => _api = api;
    private static readonly int[] PageSizes = [10, 20, 50];
    public IReadOnlyList<AlbumItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanPublish { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public int TotalAll { get; private set; }
    public int TotalImage { get; private set; }
    public int TotalVideo { get; private set; }
    public int TotalHocLieu { get; private set; }
    public string? FilterKind { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public string CurrentPathAndQuery { get; private set; } = "/admin/cms/album";
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(PageSize, 1)));
    public int FromItem => Total == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int ToItem => Math.Min(CurrentPage * PageSize, Total);
    public bool FiltersActive => FilterKind is not null;
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string Kind { get; set; } = "IMAGE";

    public async Task<IActionResult> OnGetAsync(string? kind, string? sort, string? dir, int page = 1, int pageSize = 20)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(page, kind, pageSize, sort, dir);
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id, string? returnUrl)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/albums/" + id + "/publish", token, new { });
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/admin/cms/album" : returnUrl);
    }

    public string ListUrl(int? page = null, int? pageSize = null, string? kind = null, string? sort = null, string? dir = null)
    {
        var parts = new List<string>();
        var nextKind = kind ?? FilterKind;
        if (kind == "") nextKind = null;
        var nextSize = pageSize ?? PageSize;
        var nextPage = page ?? CurrentPage;
        if (!string.IsNullOrWhiteSpace(nextKind)) parts.Add("kind=" + Uri.EscapeDataString(nextKind));
        CmsListSort.AppendParts(parts, sort ?? Sort, dir ?? Dir);
        if (nextSize != 20) parts.Add("pageSize=" + nextSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (nextPage > 1) parts.Add("page=" + nextPage.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return parts.Count == 0 ? "/admin/cms/album" : "/admin/cms/album?" + string.Join("&", parts);
    }

    public string SortUrl(string column) => ListUrl(page: 1, sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

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

    private async Task<IActionResult> LoadAsync(int page = 1, string? kind = null, int pageSize = 20, string? sort = null, string? dir = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        CanPublish = Has("cms.publish");
        FilterKind = kind is "IMAGE" or "VIDEO_LINK" or "HOC_LIEU" ? kind : null;
        Sort = CmsListSort.Normalize(sort, "title", "kind", "status");
        Dir = CmsListSort.Dir(dir);
        PageSize = PageSizes.Contains(pageSize) ? pageSize : 20;
        CurrentPage = Math.Max(1, page);
        CurrentPathAndQuery = Request.Path + Request.QueryString;
        var allTask = CountAsync(token, null);
        var imageTask = CountAsync(token, "IMAGE");
        var videoTask = CountAsync(token, "VIDEO_LINK");
        var hocLieuTask = CountAsync(token, "HOC_LIEU");
        var listTask = FetchAsync(token, CurrentPage, PageSize, FilterKind);
        await Task.WhenAll(allTask, imageTask, videoTask, hocLieuTask, listTask);
        TotalAll = allTask.Result;
        TotalImage = imageTask.Result;
        TotalVideo = videoTask.Result;
        TotalHocLieu = hocLieuTask.Result;
        var list = listTask.Result;
        Items = list.Items ?? [];
        CurrentPage = list.Page ?? CurrentPage;
        if (list.PageSize is int size && PageSizes.Contains(size)) PageSize = size;
        Total = list.Total ?? Items.Count;
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "kind" => Items.OrderBy(item => item.Kind, StringComparer.OrdinalIgnoreCase),
                "status" => Items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                _ => Items.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        return Page();
    }

    private async Task<int> CountAsync(string token, string? kind)
    {
        var result = await FetchAsync(token, 1, 1, kind);
        return result.Total ?? 0;
    }

    private async Task<ListEnvelope<AlbumItem>> FetchAsync(string token, int page, int pageSize, string? kind)
    {
        var path = $"/api/v1/admin/albums?page={page}&pageSize={pageSize}";
        if (kind is not null) path += "&kind=" + Uri.EscapeDataString(kind);
        return await _api.GetJsonAsync<ListEnvelope<AlbumItem>>(path, token)
            ?? new ListEnvelope<AlbumItem>([], page, pageSize, 0);
    }

    private bool HasView() => Has("cms.view") || Has("cms.create") || Has("cms.update") || Has("cms.publish") || Has("cms.delete");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record AlbumItem(Guid Id, string Title, string Slug, string Kind, string Status);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
    private sealed record ItemEnvelope<T>(T? Item);
}
