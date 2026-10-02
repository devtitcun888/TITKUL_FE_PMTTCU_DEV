using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class HocVienModel : PageModel
{
    private static readonly int[] PageSizes = [10, 20, 50];
    private readonly BackendApiClient _api;
    public HocVienModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<LearnerItem> Items { get; private set; } = [];
    public IReadOnlyList<OptionItem> Classes { get; private set; } = [];
    public IReadOnlyList<HamletItem> Hamlets { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int Total { get; private set; }
    public string? Query { get; private set; }
    public Guid? ClassFilter { get; private set; }
    public Guid? HamletFilter { get; private set; }
    public int? AgeFrom { get; private set; }
    public int? AgeTo { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public bool CanExport { get; private set; }
    public bool FiltersActive => Query is not null || ClassFilter is not null || HamletFilter is not null || AgeFrom is not null || AgeTo is not null;
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(PageSize, 1)));
    public int FromItem => Total == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int ToItem => Math.Min(CurrentPage * PageSize, Total);

    public async Task<IActionResult> OnGetAsync(string? q, Guid? classId, Guid? hamletId, int? ageFrom, int? ageTo, string? sort, string? dir, int page = 1, int pageSize = 20)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        CanExport = Has("learner.export");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        ClassFilter = classId;
        HamletFilter = hamletId;
        AgeFrom = ageFrom is int from && from >= 0 ? from : null;
        AgeTo = ageTo is int to && to >= 0 ? to : null;
        Sort = CmsListSort.Normalize(sort, "name", "phone", "age", "hamlet", "status");
        Dir = CmsListSort.Dir(dir);
        PageSize = PageSizes.Contains(pageSize) ? pageSize : 20;
        CurrentPage = page < 1 ? 1 : page;
        var list = await FetchAsync(token, CurrentPage, PageSize);
        var classes = await _api.GetJsonAsync<ListEnvelope<OptionItem>>("/api/v1/admin/classes?pageSize=100", token);
        var hamlets = await _api.GetJsonAsync<ListEnvelope<HamletItem>>("/api/v1/admin/hamlets", token);
        Items = list.Items ?? [];
        Total = list.Total;
        Classes = classes?.Items ?? [];
        Hamlets = hamlets?.Items ?? [];
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "phone" => Items.OrderBy(item => item.Phone, StringComparer.OrdinalIgnoreCase),
                "age" => Items.OrderBy(item => item.Age),
                "hamlet" => Items.OrderBy(item => item.HamletName, StringComparer.CurrentCultureIgnoreCase),
                "status" => Items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                _ => Items.OrderBy(item => item.FullName, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        return Page();
    }

    public string ListUrl(int? page = null, int? pageSize = null, string? q = null, Guid? classId = null, Guid? hamletId = null, int? ageFrom = null, int? ageTo = null, string? sort = null, string? dir = null)
    {
        var query = new Dictionary<string, string?>();
        var nextQ = q ?? Query;
        var nextClass = classId ?? ClassFilter;
        var nextHamlet = hamletId ?? HamletFilter;
        var nextFrom = ageFrom ?? AgeFrom;
        var nextTo = ageTo ?? AgeTo;
        var nextSize = pageSize ?? PageSize;
        var nextPage = page ?? CurrentPage;
        if (!string.IsNullOrWhiteSpace(nextQ)) query["q"] = nextQ;
        if (nextClass is Guid cls) query["classId"] = cls.ToString();
        if (nextHamlet is Guid hamlet) query["hamletId"] = hamlet.ToString();
        if (nextFrom is int from) query["ageFrom"] = from.ToString(CultureInfo.InvariantCulture);
        if (nextTo is int to) query["ageTo"] = to.ToString(CultureInfo.InvariantCulture);
        CmsListSort.Append(query, sort ?? Sort, dir ?? Dir);
        if (nextSize != 20) query["pageSize"] = nextSize.ToString(CultureInfo.InvariantCulture);
        if (nextPage > 1) query["page"] = nextPage.ToString(CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString("/admin/hoc-vien", query);
    }

    public string SortUrl(string column) => ListUrl(page: 1, sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public string ExportUrl()
    {
        var query = new Dictionary<string, string?> { ["handler"] = "Export" };
        if (!string.IsNullOrWhiteSpace(Query)) query["q"] = Query;
        if (ClassFilter is Guid cls) query["classId"] = cls.ToString();
        if (HamletFilter is Guid hamlet) query["hamletId"] = hamlet.ToString();
        if (AgeFrom is int from) query["ageFrom"] = from.ToString(CultureInfo.InvariantCulture);
        if (AgeTo is int to) query["ageTo"] = to.ToString(CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString("/admin/hoc-vien", query);
    }

    public async Task<IActionResult> OnGetExportAsync(string? q, Guid? classId, Guid? hamletId, int? ageFrom, int? ageTo)
    {
        if (!Has("learner.export")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var path = "/api/v1/admin/learners/export?";
        if (!string.IsNullOrWhiteSpace(q)) path += "q=" + Uri.EscapeDataString(q) + "&";
        if (classId is Guid cls) path += "classId=" + cls + "&";
        if (hamletId is Guid hamlet) path += "hamletId=" + hamlet + "&";
        if (ageFrom is int from) path += "ageFrom=" + from + "&";
        if (ageTo is int to) path += "ageTo=" + to + "&";
        var file = await _api.GetFileAsync(path.TrimEnd('&'), token);
        if (file.Bytes is null)
        {
            ErrorMessage = "Không xuất được Excel.";
            return await OnGetAsync(q, classId, hamletId, ageFrom, ageTo, null, null);
        }

        return File(file.Bytes, file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName ?? "hoc-vien.xlsx");
    }

    private async Task<ListEnvelope<LearnerItem>> FetchAsync(string token, int page, int pageSize)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(Query)) query["q"] = Query;
        if (ClassFilter is Guid cls) query["classId"] = cls.ToString();
        if (HamletFilter is Guid hamlet) query["hamletId"] = hamlet.ToString();
        if (AgeFrom is int from) query["ageFrom"] = from.ToString(CultureInfo.InvariantCulture);
        if (AgeTo is int to) query["ageTo"] = to.ToString(CultureInfo.InvariantCulture);
        var path = QueryHelpers.AddQueryString("/api/v1/admin/learners", query);
        var result = await _api.GetJsonAsync<ListEnvelope<LearnerItem>>(path, token);
        if (result is null)
        {
            ErrorMessage ??= "Không tải được danh sách học viên.";
            return new ListEnvelope<LearnerItem>([], 0);
        }
        return result;
    }

    private bool HasView() => Has("learner.view") || Has("learner.manage");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record LearnerItem(Guid Id, string FullName, string Phone, DateOnly BirthDate, int Age, string Gender, string HamletName, string Status);
    public sealed record OptionItem(Guid Id, string Code, string Name);
    public sealed record HamletItem(Guid Id, string Code, string Name);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items, int Total);
}
