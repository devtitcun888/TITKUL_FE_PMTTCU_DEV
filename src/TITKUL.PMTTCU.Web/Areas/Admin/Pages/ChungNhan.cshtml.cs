using System.Net.Http.Json;
using System.IO.Compression;
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
    public IReadOnlyList<IssuedItem> IssuedItems { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public string? IssuedPublicUrl { get; private set; }
    public bool CanIssue { get; private set; }
    public bool CanRevoke { get; private set; }
    public IReadOnlyList<Guid> IssuedCertificateIds { get; private set; } = [];

    [BindProperty] public Guid EnrollmentId { get; set; }
    [BindProperty] public List<Guid> SelectedEnrollmentIds { get; set; } = [];
    [BindProperty] public List<Guid> CertificateIds { get; set; } = [];
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
        if (!string.IsNullOrWhiteSpace(body?.Item?.LookupCode))
        {
            SuccessMessage = "Đã phát hành chứng nhận với mã " + body.Item.LookupCode;
            IssuedPublicUrl = "/tra-cuu/" + Uri.EscapeDataString(body.Item.LookupCode);
        }
        else
        {
            SuccessMessage = "Đã phát hành chứng nhận.";
        }
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostIssueBulkAsync(Guid id)
    {
        if (!Has("certificate.issue")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var selected = SelectedEnrollmentIds.Where(value => value != Guid.Empty).Distinct().Take(101).ToArray();
        if (selected.Length is 0 or > 100)
        {
            ErrorMessage = "Chọn từ 1 đến 100 học viên đủ điều kiện.";
            return await LoadAsync(id);
        }

        var issued = 0;
        var failed = 0;
        var certificateIds = new List<Guid>();
        foreach (var enrollmentId in selected)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/enrollments/{enrollmentId}/certificates")
            {
                Content = JsonContent.Create(new { })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString("N"));
            using var response = await _api.HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var envelope = await response.Content.ReadFromJsonAsync<ItemEnvelope<Issued>>();
                if (envelope?.Item is { Id: var certificateId } && certificateId != Guid.Empty)
                {
                    issued++;
                    certificateIds.Add(certificateId);
                }
                else failed++;
            }
            else failed++;
        }

        SuccessMessage = $"Đã phát hành {issued}/{selected.Length} chứng nhận.";
        IssuedCertificateIds = certificateIds;
        if (failed > 0) ErrorMessage = $"{failed} học viên không phát hành được. Danh sách đã được tải lại; kiểm tra điều kiện và thử lại các dòng còn đủ điều kiện.";
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostDownloadBulkAsync(Guid id)
    {
        if (!Has("certificate.issue")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        var selected = CertificateIds.Where(value => value != Guid.Empty).Distinct().Take(101).ToArray();
        if (selected.Length is 0 or > 100) return BadRequest("Chọn từ 1 đến 100 chứng nhận.");

        using var archive = new MemoryStream();
        using (var zip = new ZipArchive(archive, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var certificateId in selected)
            {
                var file = await _api.GetFileAsync($"/api/v1/admin/certificates/{certificateId}/pdf", token);
                if (file.Bytes is null) continue;
                var entryName = Path.GetFileName(file.FileName ?? certificateId.ToString("N") + ".pdf");
                var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(file.Bytes);
            }
        }

        if (archive.Length == 0) return NotFound();
        return File(archive.ToArray(), "application/zip", $"chung-nhan-lop-{id:N}.zip");
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
        CanRevoke = Has("certificate.revoke");
        var body = await _api.GetJsonAsync<EligibleEnvelope>($"/api/v1/admin/classes/{id}/certificates/eligible", token);
        Items = body?.Items ?? [];
        MinPercent = body?.MinPercent ?? 80;
        if (body is null && string.IsNullOrEmpty(ErrorMessage)) ErrorMessage = "Không tải được danh sách đủ điều kiện.";
        var issued = await _api.GetJsonAsync<ListEnvelope<IssuedItem>>($"/api/v1/admin/classes/{id}/certificates", token);
        IssuedItems = issued?.Items ?? [];
        if (issued is null && string.IsNullOrEmpty(ErrorMessage)) ErrorMessage = "Không tải được danh sách chứng nhận đã phát hành.";
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
    public sealed record IssuedItem(Guid Id, Guid EnrollmentId, string LookupCode, decimal Percent, DateOnly IssuedOn, string Status, string LearnerName, string ClassName, string ClassCode);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record Issued(Guid Id, string LookupCode);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ApiErr(string? Code, string? Message);
}
