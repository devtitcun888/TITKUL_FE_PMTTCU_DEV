using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class CmsBieuMauModel : PageModel
{
    private readonly BackendApiClient _api;
    public CmsBieuMauModel(BackendApiClient api) => _api = api;
    private static readonly int[] PageSizes = [10, 20, 50];
    public IReadOnlyList<FormItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanUpdate { get; private set; }
    public bool CanPublish { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public string CurrentPathAndQuery { get; private set; } = "/admin/cms/bieu-mau";
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(PageSize, 1)));
    public int FromItem => Total == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int ToItem => Math.Min(CurrentPage * PageSize, Total);
    public bool FiltersActive => Query is not null;

    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Code { get; set; }
    [BindProperty] public string? Summary { get; set; }
    [BindProperty] public DateOnly? EffectiveFrom { get; set; }
    [BindProperty] public DateOnly? ExpiresOn { get; set; }
    [BindProperty] public IFormFile? Upload { get; set; }
    [BindProperty] public string ViewMode { get; set; } = "AUTO";

    public async Task<IActionResult> OnGetAsync(string? q, string? sort, string? dir, int page = 1, int pageSize = 20)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(page, q, pageSize, sort, dir);
    }

    public string ListUrl(int? page = null, int? pageSize = null, string? q = null, string? sort = null, string? dir = null)
    {
        var parts = new List<string>();
        var nextQ = q ?? Query;
        var nextSize = pageSize ?? PageSize;
        var nextPage = page ?? CurrentPage;
        if (!string.IsNullOrWhiteSpace(nextQ)) parts.Add("q=" + Uri.EscapeDataString(nextQ));
        CmsListSort.AppendParts(parts, sort ?? Sort, dir ?? Dir);
        if (nextSize != 20) parts.Add("pageSize=" + nextSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (nextPage > 1) parts.Add("page=" + nextPage.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return parts.Count == 0 ? "/admin/cms/bieu-mau" : "/admin/cms/bieu-mau?" + string.Join("&", parts);
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
            ErrorMessage = "Tệp biểu mẫu không được vượt quá 20 MiB.";
            return await LoadAsync();
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(Title), "title");
        content.Add(new StringContent(ViewMode), "viewMode");
        if (!string.IsNullOrWhiteSpace(Code)) content.Add(new StringContent(Code), "code");
        if (!string.IsNullOrWhiteSpace(Summary)) content.Add(new StringContent(Summary), "summary");
        if (EffectiveFrom is DateOnly effectiveFrom) content.Add(new StringContent(effectiveFrom.ToString("yyyy-MM-dd")), "effectiveFrom");
        if (ExpiresOn is DateOnly expiresOn) content.Add(new StringContent(expiresOn.ToString("yyyy-MM-dd")), "expiresOn");
        await using var stream = Upload.OpenReadStream();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(Upload.ContentType) ? "application/octet-stream" : Upload.ContentType);
        content.Add(file, "file", Upload.FileName);
        var response = await _api.PostMultipartAsync("/api/v1/admin/forms", token, content);
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không lưu được biểu mẫu.";
        else
        {
            var created = await response.Content.ReadFromJsonAsync<ItemEnvelope<FormItem>>();
            SuccessMessage = created?.Item?.Code is { Length: > 0 } code
                ? $"Đã lưu biểu mẫu. Mã được hệ thống cấp: {code}."
                : "Đã lưu biểu mẫu. Mã được hệ thống cấp và có trong danh sách.";
        }
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id, string? returnUrl)
    {
        if (!Has("cms.publish")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/forms/" + id + "/publish", token, new { });
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/admin/cms/bieu-mau" : returnUrl);
    }

    public async Task<IActionResult> OnPostViewModeAsync(Guid id, string mode)
    {
        if (!Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/forms/{id}/view-mode", token, new { mode });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không cập nhật được chế độ xem. Chỉ PDF mới hỗ trợ xem trực tiếp.";
        return await LoadAsync(CurrentPage, Query, PageSize, Sort, Dir);
    }

    private async Task<IActionResult> LoadAsync(int page = 1, string? q = null, int pageSize = 20, string? sort = null, string? dir = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.create");
        CanUpdate = Has("cms.update");
        CanPublish = Has("cms.publish");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        Sort = CmsListSort.Normalize(sort, "code", "title", "published", "expires", "status");
        Dir = CmsListSort.Dir(dir);
        PageSize = PageSizes.Contains(pageSize) ? pageSize : 20;
        CurrentPage = Math.Max(1, page);
        CurrentPathAndQuery = Request.Path + Request.QueryString;
        var list = await _api.GetJsonAsync<ListEnvelope<FormItem>>($"/api/v1/admin/forms?page={CurrentPage}&pageSize={PageSize}&q={Uri.EscapeDataString(Query ?? "")}", token);
        Items = list?.Items ?? [];
        CurrentPage = list?.Page ?? CurrentPage;
        if (list?.PageSize is int size && PageSizes.Contains(size)) PageSize = size;
        Total = list?.Total ?? Items.Count;
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "code" => Items.OrderBy(item => item.Code, StringComparer.CurrentCultureIgnoreCase),
                "published" => Items.OrderBy(item => item.PublishedAt ?? DateTimeOffset.MinValue),
                "expires" => Items.OrderBy(item => item.ExpiresOn ?? DateOnly.MaxValue),
                "status" => Items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                _ => Items.OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.create") || Has("cms.update") || Has("cms.publish") || Has("cms.delete");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record FormItem(Guid Id, string? Code, string Title, string Status, DateTimeOffset? PublishedAt = null, DateOnly? EffectiveFrom = null, DateOnly? ExpiresOn = null, string MimeType = "application/octet-stream", string ViewMode = "AUTO");
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int? Page, int? PageSize, int? Total);
}
