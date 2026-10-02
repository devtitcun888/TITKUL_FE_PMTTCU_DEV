using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;
using TITKUL.PMTTCU.Web.Areas.Admin;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class ChuongTrinhModel : PageModel
{
    private readonly BackendApiClient _api;

    public ChuongTrinhModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<Item> Items { get; private set; } = [];
    public IReadOnlyList<CategoryOption> Categories { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool CanManage { get; private set; }
    public bool Editing => Id.HasValue;
    public bool ShowModal { get; private set; }
    public string? Query { get; private set; }
    public string? StatusFilter { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public int TotalAll { get; private set; }
    public int TotalActive { get; private set; }
    public int TotalDraft { get; private set; }
    public int TotalInactive { get; private set; }
    public bool FiltersActive => Query is not null || StatusFilter is not null;

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string Goal { get; set; } = "";
    [BindProperty] public string Status { get; set; } = "ACTIVE";
    [BindProperty] public Guid? CategoryId { get; set; }
    [BindProperty] public string? Slug { get; set; }
    [BindProperty] public string? Summary { get; set; }
    [BindProperty] public string? ContentHtml { get; set; }
    [BindProperty] public string? CoverKey { get; set; }
    [BindProperty] public bool Public { get; set; }
    [BindProperty] public string? SeoTitle { get; set; }
    [BindProperty] public string? SeoDescription { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, bool create = false, string? q = null, string? status = null, string? sort = null, string? dir = null)
    {
        ShowModal = create || id.HasValue;
        return await LoadAsync(id, q: q, status: status, sort: sort, dir: dir);
    }

    public string ListUrl(string? q = null, string? status = null, string? sort = null, string? dir = null)
    {
        var parts = new List<string>();
        var nextQ = q ?? Query;
        var nextStatus = status ?? StatusFilter;
        if (status == "") nextStatus = null;
        if (!string.IsNullOrWhiteSpace(nextQ)) parts.Add("q=" + Uri.EscapeDataString(nextQ));
        if (!string.IsNullOrWhiteSpace(nextStatus)) parts.Add("status=" + Uri.EscapeDataString(nextStatus));
        CmsListSort.AppendParts(parts, sort ?? Sort, dir ?? Dir);
        return parts.Count == 0 ? "/admin/chuong-trinh" : "/admin/chuong-trinh?" + string.Join("&", parts);
    }

    public string SortUrl(string column) => ListUrl(sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { name = Name, goal = Goal, status = Status, categoryId = CategoryId, slug = Slug, summary = Summary, contentHtml = ContentHtml, coverKey = CoverKey, @public = Public, seoTitle = SeoTitle, seoDescription = SeoDescription };
        var response = Id is Guid id
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/programs/{id}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/programs", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được chương trình. Kiểm tra mã trùng hoặc dữ liệu bắt buộc.";
            ShowModal = true;
            return await LoadAsync(null, hydrateDetail: false, q: Query, status: StatusFilter, sort: Sort, dir: Dir);
        }

        return Redirect("/admin/chuong-trinh");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Delete, $"/api/v1/admin/programs/{id}", token, new { });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không xóa được. Chương trình còn lớp thì phải giữ lại.";
            return await LoadAsync(null, q: Query, status: StatusFilter, sort: Sort, dir: Dir);
        }

        return Redirect("/admin/chuong-trinh");
    }

    private bool HasManage() => Has("education.manage");
    private bool HasView() => Has("education.manage") || Has("education.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(Guid? id, bool hydrateDetail = true, string? q = null, string? status = null, string? sort = null, string? dir = null)
    {
        CanManage = HasManage();
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        StatusFilter = status is "ACTIVE" or "DRAFT" or "INACTIVE" ? status : null;
        Sort = CmsListSort.Normalize(sort, "code", "name", "status");
        Dir = CmsListSort.Dir(dir);
        var body = await _api.GetJsonAsync<ListEnvelope<Item>>("/api/v1/admin/programs?pageSize=100", token);
        var categories = await _api.GetJsonAsync<ListEnvelope<CategoryOption>>("/api/v1/admin/categories?kind=CHUONG_TRINH_HOC", token);
        Categories = (categories?.Items ?? []).Where(item => item.Active).ToArray();
        if (body is null) ErrorMessage ??= "Không tải được danh sách.";
        var all = body?.Items ?? [];
        TotalAll = all.Count;
        TotalActive = all.Count(item => item.Status == "ACTIVE");
        TotalDraft = all.Count(item => item.Status == "DRAFT");
        TotalInactive = all.Count(item => item.Status == "INACTIVE");
        IEnumerable<Item> filtered = all;
        if (StatusFilter is not null) filtered = filtered.Where(item => item.Status == StatusFilter);
        if (Query is not null)
        {
            filtered = filtered.Where(item =>
                item.Code.Contains(Query, StringComparison.CurrentCultureIgnoreCase)
                || item.Name.Contains(Query, StringComparison.CurrentCultureIgnoreCase));
        }
        Items = filtered.ToArray();
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "code" => Items.OrderBy(item => item.Code, StringComparer.CurrentCultureIgnoreCase),
                "status" => Items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                _ => Items.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        if (hydrateDetail && id is Guid editId)
        {
            var detail = await _api.GetJsonAsync<ItemEnvelope<Detail>>($"/api/v1/admin/programs/{editId}", token);
            if (detail?.Item is Detail item)
            {
                Id = item.Id;
                Code = item.Code;
                Name = item.Name;
                Goal = item.Goal;
                Status = item.Status;
                CategoryId = item.CategoryId;
                Slug = item.Slug;
                Summary = item.Summary;
                ContentHtml = item.ContentHtml;
                CoverKey = item.CoverKey;
                Public = item.Public;
                SeoTitle = item.SeoTitle;
                SeoDescription = item.SeoDescription;
            }
        }

        return Page();
    }

    public string CategoryName(Guid? id) => Categories.FirstOrDefault(item => item.Id == id)?.Name ?? "Chưa phân nhóm";
    public sealed record Item(Guid Id, string Code, string Name, string Status, Guid? CategoryId = null, string? Slug = null, bool Public = false);
    public sealed record CategoryOption(Guid Id, string Name, string Slug, bool Active);
    private sealed record Detail(Guid Id, string Code, string Name, string Goal, string Status, Guid? CategoryId = null, string? Slug = null, string? Summary = null, string? ContentHtml = null, string? CoverKey = null, bool Public = false, string? SeoTitle = null, string? SeoDescription = null);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ItemEnvelope<T>(T? Item);
}
