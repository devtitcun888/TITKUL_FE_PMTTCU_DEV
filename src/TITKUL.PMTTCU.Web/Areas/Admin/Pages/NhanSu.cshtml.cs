using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class NhanSuModel : PageModel
{
    private static readonly int[] PageSizes = [10, 20, 50];
    private readonly BackendApiClient _api;

    public NhanSuModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<CollaboratorItem> Items { get; private set; } = [];
    public IReadOnlyList<StaffOption> Staff { get; private set; } = [];
    public CollaboratorItem? EditingItem { get; private set; }
    public int Total { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public bool ShowModal { get; private set; }
    public bool ShowImport { get; private set; }
    public string? Query { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public bool FiltersActive => Query is not null;
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(PageSize, 1)));
    public int FromItem => Total == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int ToItem => Math.Min(CurrentPage * PageSize, Total);

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public string FullName { get; set; } = "";
    [BindProperty] public bool Female { get; set; }
    [BindProperty] public int? BirthYear { get; set; }
    [BindProperty] public string? WorkUnit { get; set; }
    [BindProperty] public string? Position { get; set; }
    [BindProperty] public string? Phone { get; set; }
    [BindProperty] public Guid? LinkedUserId { get; set; }
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public IFormFile? ImportFile { get; set; }

    public async Task<IActionResult> OnGetAsync(string? q, string? Q, string? sort, string? dir, int page = 1, int pageSize = 20, Guid? id = null, bool create = false, bool import = false)
    {
        var loaded = await LoadAsync(q ?? Q, sort, dir, page, pageSize, id);
        ShowModal = create || EditingItem is not null;
        ShowImport = import;
        return loaded;
    }

    public string ListUrl(int? page = null, int? pageSize = null, string? q = null, string? sort = null, string? dir = null)
    {
        var query = new Dictionary<string, string?>();
        var nextQ = q ?? Query;
        var nextSize = pageSize ?? PageSize;
        var nextPage = page ?? CurrentPage;
        if (!string.IsNullOrWhiteSpace(nextQ)) query["q"] = nextQ;
        CmsListSort.Append(query, sort ?? Sort, dir ?? Dir);
        if (nextSize != 20) query["pageSize"] = nextSize.ToString(CultureInfo.InvariantCulture);
        if (nextPage > 1) query["page"] = nextPage.ToString(CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString("/admin/nhan-su", query);
    }

    public string SortUrl(string column) => ListUrl(page: 1, sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var body = new { fullName = FullName, female = Female, birthYear = BirthYear, workUnit = WorkUnit,
            position = Position, phone = Phone, linkedUserId = LinkedUserId, note = Note };
        var response = Id is Guid id
            ? await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/collaborators/{id}", token, body)
            : await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/collaborators", token, body);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được hồ sơ. Hãy kiểm tra họ tên và tài khoản liên kết.";
            ShowModal = true;
            return await LoadAsync(Query, Sort, Dir, CurrentPage, PageSize, Id);
        }

        return Redirect("/admin/nhan-su");
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (ImportFile is null || ImportFile.Length == 0)
        {
            ErrorMessage = "Chọn file Excel .xlsx theo biểu mẫu Cộng tác viên, GV.";
            ShowImport = true;
            return await LoadAsync(Query, Sort, Dir, CurrentPage, PageSize, null);
        }

        using var content = new MultipartFormDataContent();
        await using var stream = ImportFile.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(ImportFile.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", ImportFile.FileName);
        var response = await _api.PostMultipartAsync("/api/v1/admin/collaborators/import", token, content);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không nhập được file Excel. Hãy dùng biểu mẫu Cộng tác viên, GV.";
            ShowImport = true;
            return await LoadAsync(Query, Sort, Dir, CurrentPage, PageSize, null);
        }

        var imported = await response.Content.ReadFromJsonAsync<ImportResult>();
        SuccessMessage = $"Đã nhập {imported?.Created ?? 0} hồ sơ; bỏ qua {imported?.SkippedBlankRows ?? 0} dòng trống.";
        return await LoadAsync(null, null, null, 1, 20, null);
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync("/api/v1/admin/collaborators/export", token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName ?? "cong-tac-vien-giao-vien.xlsx");
    }

    private bool HasManage() => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains("education.manage") == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private async Task<IActionResult> LoadAsync(string? q, string? sort, string? dir, int page, int pageSize, Guid? id)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        Sort = CmsListSort.Normalize(sort, "name", "gender", "year", "unit", "position", "phone", "account");
        Dir = CmsListSort.Dir(dir);
        PageSize = PageSizes.Contains(pageSize) ? pageSize : 20;
        CurrentPage = page < 1 ? 1 : page;
        var pathQuery = new Dictionary<string, string?>
        {
            ["page"] = CurrentPage.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = PageSize.ToString(CultureInfo.InvariantCulture)
        };
        if (Query is not null) pathQuery["q"] = Query;
        var result = await _api.GetJsonAsync<PagedEnvelope<CollaboratorItem>>(QueryHelpers.AddQueryString("/api/v1/admin/collaborators", pathQuery), token);
        if (result is null) ErrorMessage ??= "Không tải được danh sách cộng tác viên.";
        Items = result?.Items ?? [];
        Total = result?.Total ?? 0;
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "gender" => Items.OrderBy(item => item.Female),
                "year" => Items.OrderBy(item => item.BirthYear ?? 0),
                "unit" => Items.OrderBy(item => item.WorkUnit ?? "", StringComparer.CurrentCultureIgnoreCase),
                "position" => Items.OrderBy(item => item.Position ?? "", StringComparer.CurrentCultureIgnoreCase),
                "phone" => Items.OrderBy(item => item.Phone ?? "", StringComparer.OrdinalIgnoreCase),
                "account" => Items.OrderBy(item => item.LinkedUsername ?? "", StringComparer.OrdinalIgnoreCase),
                _ => Items.OrderBy(item => item.FullName, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        var staff = await _api.GetJsonAsync<ListEnvelope<StaffOption>>("/api/v1/admin/education/staff", token);
        Staff = staff?.Items ?? [];
        if (id is Guid editId)
        {
            var detail = await _api.GetJsonAsync<ItemEnvelope<CollaboratorItem>>($"/api/v1/admin/collaborators/{editId}", token);
            if (detail?.Item is CollaboratorItem item)
            {
                EditingItem = item;
                Id = item.Id;
                FullName = item.FullName;
                Female = item.Female;
                BirthYear = item.BirthYear;
                WorkUnit = item.WorkUnit;
                Position = item.Position;
                Phone = item.Phone;
                LinkedUserId = item.LinkedUserId;
                Note = item.Note;
            }
        }

        return Page();
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage? response)
    {
        if (response is null) return null;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorBody>();
            return string.IsNullOrWhiteSpace(error?.Message) ? null : error.Message;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public sealed record CollaboratorItem(Guid Id, string FullName, bool Female, int? BirthYear, string? WorkUnit,
        string? Position, string? Phone, Guid? LinkedUserId, string? LinkedUsername, string? Note);
    public sealed record StaffOption(Guid Id, string Username, IReadOnlyList<string>? Roles);
    private sealed record PagedEnvelope<T>(IReadOnlyList<T>? Items, int Total);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ImportResult(int Created, int SkippedBlankRows);
    private sealed record ApiErrorBody(string? Code, string? Message);
}
