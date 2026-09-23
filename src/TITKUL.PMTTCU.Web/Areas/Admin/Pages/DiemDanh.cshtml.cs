using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class DiemDanhModel : PageModel
{
    private readonly BackendApiClient _api;
    public DiemDanhModel(BackendApiClient api) => _api = api;
    public Sheet? Item { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Query { get; private set; }

    [BindProperty] public List<Row> Marks { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid buoiId, string? q)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(buoiId, q);
    }

    public async Task<IActionResult> OnGetQrAsync(Guid buoiId)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync($"/api/v1/admin/sessions/{buoiId}/attendance-qr", token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, file.ContentType ?? "image/png", file.FileName ?? "diem-danh.png");
    }

    public async Task<IActionResult> OnPostFillAsync(Guid buoiId, string fill)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/sessions/{buoiId}/attendance", token, new { fillAll = fill });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được điểm danh hàng loạt.";
            return await LoadAsync(buoiId, null);
        }

        return Redirect($"/admin/diem-danh/{buoiId}");
    }

    public async Task<IActionResult> OnPostAsync(Guid buoiId, string? q)
    {
        if (!HasManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Put, $"/api/v1/admin/sessions/{buoiId}/attendance", token, new
        {
            items = Marks.Select(item => new { enrollmentId = item.EnrollmentId, status = item.Status })
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không lưu được điểm danh.";
            return await LoadAsync(buoiId, q);
        }

        return Redirect($"/admin/diem-danh/{buoiId}");
    }

    private async Task<IActionResult> LoadAsync(Guid buoiId, string? q)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        Query = q;
        var body = await _api.GetJsonAsync<ItemEnvelope>($"/api/v1/admin/sessions/{buoiId}/attendance", token);
        Item = body?.Item;
        if (Item is null) ErrorMessage ??= "Không tải được danh sách điểm danh.";
        else if (!string.IsNullOrWhiteSpace(q))
        {
            Item = Item with { Items = Item.Items.Where(item => item.FullName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToArray() };
        }

        if (Item is not null && Marks.Count == 0)
        {
            Marks = Item.Items.Select(item => new Row { EnrollmentId = item.EnrollmentId, Status = item.Status ?? "VANG" }).ToList();
        }

        return Page();
    }

    private bool HasManage() => Has("attendance.manage");
    private bool HasView() => Has("attendance.manage") || Has("attendance.view");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage? response)
    {
        if (response is null) return null;
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ApiErr>();
            return string.IsNullOrWhiteSpace(body?.Message) ? null : body.Message;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public sealed record Line(Guid EnrollmentId, string FullName, string Phone, string? Status);
    public sealed record Sheet(Guid SessionId, Guid ClassId, string ClassName, string SessionTitle, DateTimeOffset StartAt, DateTimeOffset EndAt, string SessionStatus, bool CanEdit, bool CheckInOpen, string CheckInUrl, string Pin, IReadOnlyList<Line> Items, int Present, int Absent, int Excused);
    public sealed class Row
    {
        public Guid EnrollmentId { get; set; }
        public string Status { get; set; } = "VANG";
    }

    private sealed record ItemEnvelope(Sheet? Item);
    private sealed record ApiErr(string? Code, string? Message);
}
