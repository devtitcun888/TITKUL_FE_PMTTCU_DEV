using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Areas.Admin;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class CauLacBoModel : PageModel
{
    private readonly BackendApiClient _api;
    public CauLacBoModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<ClubItem> Items { get; private set; } = [];
    public bool CanManage { get; private set; }
    public bool ShowModal { get; private set; }
    public ClubItem? EditingItem { get; private set; }
    public string? Query { get; private set; }
    public string? StatusFilter { get; private set; }
    public string? Sort { get; private set; }
    public string Dir { get; private set; } = "asc";
    public int TotalAll { get; private set; }
    public int TotalActive { get; private set; }
    public int TotalInactive { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public bool FiltersActive => Query is not null || StatusFilter is not null;

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string? RegularSchedule { get; set; }
    [BindProperty] public string Status { get; set; } = "ACTIVE";

    public async Task<IActionResult> OnGetAsync(string? q, string? status, string? sort, string? dir, Guid? edit, bool create = false)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var page = await LoadAsync(q, status, sort, dir, edit);
        ShowModal = create || EditingItem is not null;
        return page;
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
        return parts.Count == 0 ? "/admin/cau-lac-bo" : "/admin/cau-lac-bo?" + string.Join("&", parts);
    }

    public string SortUrl(string column) => ListUrl(sort: column, dir: CmsListSort.NextDir(Sort, column, Dir));

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!Has("education.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/clubs", token, Payload());
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không tạo được câu lạc bộ. Kiểm tra tên và trạng thái.";
            ShowModal = true;
        }
        else
        {
            var created = await response.Content.ReadFromJsonAsync<ItemEnvelope<ClubItem>>();
            SuccessMessage = created?.Item?.Code is { Length: > 0 } code
                ? $"Đã tạo câu lạc bộ. Mã được hệ thống cấp: {code}."
                : "Đã tạo câu lạc bộ. Mã được hệ thống cấp và hiển thị trong danh sách.";
        }
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, string name, string? description, string? location, string? regularSchedule, string status)
    {
        if (!Has("education.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var payload = new { name, description, location, regularSchedule, status };
        var response = await _api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/clubs/" + id, token, payload);
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được câu lạc bộ. Kiểm tra mã và nội dung.";
            ShowModal = true;
            return await LoadAsync(edit: id);
        }
        SuccessMessage = "Đã cập nhật câu lạc bộ.";
        return await LoadAsync();
    }

    private async Task<IActionResult> LoadAsync(string? q = null, string? status = null, string? sort = null, string? dir = null, Guid? edit = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanManage = Has("education.manage");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        StatusFilter = status is "ACTIVE" or "INACTIVE" ? status : null;
        Sort = CmsListSort.Normalize(sort, "code", "name", "location", "schedule", "status");
        Dir = CmsListSort.Dir(dir);
        var body = await _api.GetJsonAsync<ListEnvelope<ClubItem>>("/api/v1/admin/clubs?page=1&pageSize=100", token);
        var all = body?.Items ?? [];
        if (body is null && string.IsNullOrEmpty(ErrorMessage)) ErrorMessage = "Không tải được danh sách câu lạc bộ.";
        TotalAll = all.Count;
        TotalActive = all.Count(item => item.Status == "ACTIVE");
        TotalInactive = all.Count(item => item.Status == "INACTIVE");
        IEnumerable<ClubItem> filtered = all;
        if (StatusFilter is not null) filtered = filtered.Where(item => item.Status == StatusFilter);
        if (Query is not null)
        {
            filtered = filtered.Where(item =>
                item.Code.Contains(Query, StringComparison.CurrentCultureIgnoreCase)
                || item.Name.Contains(Query, StringComparison.CurrentCultureIgnoreCase)
                || (item.Location?.Contains(Query, StringComparison.CurrentCultureIgnoreCase) ?? false));
        }
        Items = filtered.ToArray();
        if (Sort is not null)
        {
            Items = CmsListSort.Order(Items, Dir, Sort switch
            {
                "code" => Items.OrderBy(item => item.Code, StringComparer.CurrentCultureIgnoreCase),
                "location" => Items.OrderBy(item => item.Location ?? "", StringComparer.CurrentCultureIgnoreCase),
                "schedule" => Items.OrderBy(item => item.RegularSchedule ?? "", StringComparer.CurrentCultureIgnoreCase),
                "status" => Items.OrderBy(item => item.Status, StringComparer.OrdinalIgnoreCase),
                _ => Items.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            });
        }
        if (edit is Guid editId) EditingItem = all.FirstOrDefault(item => item.Id == editId) ?? Items.FirstOrDefault(item => item.Id == editId);
        return Page();
    }

    private object Payload() => new { name = Name, description = Description, location = Location, regularSchedule = RegularSchedule, status = Status };
    private bool HasView() => Has("education.manage") || Has("education.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record ClubItem(Guid Id, string Code, string Name, string? Description, string? Location, string? RegularSchedule, string Status);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
}
