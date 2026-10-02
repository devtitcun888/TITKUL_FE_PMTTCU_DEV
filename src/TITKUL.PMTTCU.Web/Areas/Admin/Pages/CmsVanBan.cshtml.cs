using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsVanBanModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsVanBanModel(BackendApiClient api) => _api = api;
    private static readonly int[] PageSizes = [10, 20, 50];
    public IReadOnlyList<DocumentItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanUpdate { get; private set; }
    public bool CanPublish { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public string? FieldFilter { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public string CurrentPathAndQuery { get; private set; } = "/admin/cms/van-ban";
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(PageSize, 1)));
    public int FromItem => Total == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int ToItem => Math.Min(CurrentPage * PageSize, Total);
    public bool FiltersActive => Query is not null || FieldFilter is not null;

    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Symbol { get; set; }
    [BindProperty] public string? Field { get; set; }
    [BindProperty] public string? Issuer { get; set; }
    [BindProperty] public DateOnly? IssuedOn { get; set; }
    [BindProperty] public DateOnly? EffectiveFrom { get; set; }
    [BindProperty] public DateOnly? ExpiresOn { get; set; }
    [BindProperty] public IFormFile? Upload { get; set; }
    [BindProperty] public string ViewMode { get; set; } = "AUTO";

    public async Task<IActionResult> OnGetAsync(string? q, string? field, string? sort, string? dir, int page = 1, int pageSize = 20)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(page, q, field, pageSize, sort, dir);
    }

    public string ListUrl(int? page = null, int? pageSize = null, string? q = null, string? field = null, string? sort = null, string? dir = null)
    {
        var parts = new List<string>();
        var nextQ = q ?? Query;
        var nextField = field ?? FieldFilter;
        var nextSize = pageSize ?? PageSize;
        var nextPage = page ?? CurrentPage;
        if (!string.IsNullOrWhiteSpace(nextQ)) parts.Add("q=" + Uri.EscapeDataString(nextQ));
        if (!string.IsNullOrWhiteSpace(nextField)) parts.Add("field=" + Uri.EscapeDataString(nextField));
        CmsListSort.AppendParts(parts, sort ?? Sort, dir ?? Dir);
        if (nextSize != 20) parts.Add("pageSize=" + nextSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (nextPage > 1) parts.Add("page=" + nextPage.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return parts.Count == 0 ? "/admin/cms/van-ban" : "/admin/cms/van-ban?" + string.Join("&", parts);
    }

    public string SortUrl(string column) => ListUrl(page: 1, sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Has("cms.create")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (Upload is null)
        {
            ErrorMessage = "Chọn file PDF, Word hoặc Excel.";
            return await LoadAsync();
        }
        if (Upload.Length > 20 * 1024 * 1024)
        {
            ErrorMessage = "Tệp văn bản không được vượt quá 20 MiB.";
            return await LoadAsync();
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(Title), "title");
        content.Add(new StringContent(ViewMode), "viewMode");
        if (!string.IsNullOrWhiteSpace(Symbol)) content.Add(new StringContent(Symbol), "symbol");
        if (!string.IsNullOrWhiteSpace(Field)) content.Add(new StringContent(Field), "field");
        if (!string.IsNullOrWhiteSpace(Issuer)) content.Add(new StringContent(Issuer), "issuer");
        if (IssuedOn is DateOnly issuedOn) content.Add(new StringContent(issuedOn.ToString("yyyy-MM-dd")), "issuedOn");
        if (EffectiveFrom is DateOnly effectiveFrom) content.Add(new StringContent(effectiveFrom.ToString("yyyy-MM-dd")), "effectiveFrom");
        if (ExpiresOn is DateOnly expiresOn) content.Add(new StringContent(expiresOn.ToString("yyyy-MM-dd")), "expiresOn");
        await using var stream = Upload.OpenReadStream();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(Upload.ContentType) ? "application/octet-stream" : Upload.ContentType);
        content.Add(file, "file", Upload.FileName);
        var response = await _api.PostMultipartAsync("/api/v1/admin/documents", token, content);
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không lưu được văn bản. Kiểm tra loại file và dung lượng.";
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id, string? returnUrl)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/documents/" + id + "/publish", token, new { });
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/admin/cms/van-ban" : returnUrl);
    }

    public async Task<IActionResult> OnPostViewModeAsync(Guid id, string mode)
    {
        if (!Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/documents/{id}/view-mode", token, new { mode });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không cập nhật được chế độ xem. Chỉ PDF mới hỗ trợ xem trực tiếp.";
        return await LoadAsync(CurrentPage, Query, FieldFilter, PageSize, Sort, Dir);
    }

    private async Task<IActionResult> LoadAsync(int page = 1, string? q = null, string? field = null, int pageSize = 20, string? sort = null, string? dir = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        CanUpdate = Has("cms.update");
        CanPublish = Has("cms.publish");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        FieldFilter = string.IsNullOrWhiteSpace(field) ? null : field.Trim();
        Sort = CmsListSort.Normalize(sort, "title", "issued", "expires", "field", "status");
        Dir = CmsListSort.Dir(dir);
        PageSize = PageSizes.Contains(pageSize) ? pageSize : 20;
        CurrentPage = Math.Max(1, page);
        CurrentPathAndQuery = Request.Path + Request.QueryString;
        var path = $"/api/v1/admin/documents?page={CurrentPage}&pageSize={PageSize}&q={Uri.EscapeDataString(Query ?? "")}&field={Uri.EscapeDataString(FieldFilter ?? "")}";
        var list = await _api.GetJsonAsync<ListEnvelope<DocumentItem>>(path, token);
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? CurrentPage;
        if (list?.PageSize is int size && PageSizes.Contains(size)) PageSize = size;
        Total = list?.Total ?? Items.Count;
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "issued" => Items.OrderBy(item => item.IssuedOn ?? DateOnly.MinValue),
                "expires" => Items.OrderBy(item => item.ExpiresOn ?? DateOnly.MaxValue),
                "field" => Items.OrderBy(item => item.Field, StringComparer.CurrentCultureIgnoreCase),
                "status" => Items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                _ => Items.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create") || Has("cms.update") || Has("cms.publish") || Has("cms.delete");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record DocumentItem(Guid Id, string? Symbol, string Title, string? Issuer, DateOnly? IssuedOn, string? Field, string FileName, string Status, DateOnly? EffectiveFrom = null, DateOnly? ExpiresOn = null, string MimeType = "application/octet-stream", string ViewMode = "AUTO");
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
