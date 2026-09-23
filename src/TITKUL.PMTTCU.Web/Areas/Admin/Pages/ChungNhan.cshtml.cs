using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public class ChungNhanModel : PageModel
{
    private readonly BackendApiClient _api;
    public ChungNhanModel(BackendApiClient api) => _api = api;
    public Guid ClassId { get; private set; }
    public decimal MinPercent { get; private set; }
    public IReadOnlyList<EligibleItem> Items { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public bool CanIssue { get; private set; }

    [BindProperty] public Guid EnrollmentId { get; set; }
    [BindProperty] public Guid CertificateId { get; set; }
    [BindProperty] public string RevokeReason { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostIssueAsync(Guid id)
    {
        if (!Has("certificate.issue")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/enrollments/{EnrollmentId}/certificates")
        {
            Content = JsonContent.Create(new { })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var response = await _api.HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không phát hành được.";
            return await LoadAsync(id);
        }

        var body = await response.Content.ReadFromJsonAsync<ItemEnvelope<Issued>>();
        SuccessMessage = "Đã cấp mã " + body?.Item?.LookupCode;
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid id)
    {
        if (!Has("certificate.revoke")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/certificates/{CertificateId}/revoke", token, new { reason = RevokeReason });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = await ReadErrorAsync(response) ?? "Không thu hồi được.";
            return await LoadAsync(id);
        }

        SuccessMessage = "Đã thu hồi chứng nhận.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnGetPdfAsync(Guid id, Guid certificateId)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var file = await _api.GetFileAsync($"/api/v1/admin/certificates/{certificateId}/pdf", token);
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, "application/pdf", file.FileName ?? "chung-nhan.pdf");
    }

    private async Task<IActionResult> LoadAsync(Guid id)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        ClassId = id;
        CanIssue = Has("certificate.issue");
        var body = await _api.GetJsonAsync<EligibleEnvelope>($"/api/v1/admin/classes/{id}/certificates/eligible", token);
        Items = body?.Items ?? [];
        MinPercent = body?.MinPercent ?? 80;
        if (body is null && string.IsNullOrEmpty(ErrorMessage)) ErrorMessage = "Không tải được danh sách đủ điều kiện.";
        return Page();
    }

    private bool HasView() => Has("certificate.issue") || Has("certificate.revoke") || Has("education.view");
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

    public sealed record EligibleItem(Guid EnrollmentId, string FullName, decimal Percent, bool Eligible, string? Reason);
    private sealed record EligibleEnvelope(IReadOnlyList<EligibleItem>? Items, decimal MinPercent);
    private sealed record Issued(string LookupCode);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ApiErr(string? Code, string? Message);
}
