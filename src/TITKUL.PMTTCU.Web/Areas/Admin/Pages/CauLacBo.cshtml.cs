using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class CauLacBoModel : PageModel
{
    private readonly BackendApiClient _api;
    public CauLacBoModel(BackendApiClient api) => _api = api;

    public IReadOnlyList<ClubItem> Items { get; private set; } = [];
    public bool CanManage { get; private set; }
    public string? Query { get; private set; }
    public string? StatusFilter { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string? RegularSchedule { get; set; }
    [BindProperty] public string Status { get; set; } = "ACTIVE";

    public async Task<IActionResult> OnGetAsync(string? q, string? status)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(q, status);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!Has("education.manage")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/clubs", token, Payload());
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không tạo được câu lạc bộ. Kiểm tra tên và trạng thái.";
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
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không lưu được câu lạc bộ. Kiểm tra mã và nội dung.";
        else SuccessMessage = "Đã cập nhật câu lạc bộ.";
        return await LoadAsync();
    }

    private async Task<IActionResult> LoadAsync(string? q = null, string? status = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanManage = Has("education.manage");
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        StatusFilter = status is "ACTIVE" or "INACTIVE" ? status : null;
        var path = "/api/v1/admin/clubs?page=1&pageSize=100";
        if (Query is not null) path += "&q=" + Uri.EscapeDataString(Query);
        if (StatusFilter is not null) path += "&status=" + Uri.EscapeDataString(StatusFilter);
        var body = await _api.GetJsonAsync<ListEnvelope<ClubItem>>(path, token);
        Items = body?.Items ?? [];
        if (body is null && string.IsNullOrEmpty(ErrorMessage)) ErrorMessage = "Không tải được danh sách câu lạc bộ.";
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
