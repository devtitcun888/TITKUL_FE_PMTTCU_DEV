using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class LienHeChiTietModel(BackendApiClient api) : PageModel
{
    public AdminThread? Thread { get; private set; }
    public string? Error { get; private set; }
    public bool CanEdit { get; private set; }
    [BindProperty] public string Body { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!HasView()) return Redirect("/admin/khong-quyen");
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostStatusAsync(Guid id, string status)
    {
        if (!Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        await api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/contacts/" + id, token, new { status });
        return RedirectToPage("/LienHeChiTiet", new { id });
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!Has("cms.update")) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Body) || Body.Trim().Length is < 3 or > 4000)
        {
            Error = "Nội dung phản hồi phải có từ 3 đến 4.000 ký tự.";
            return await LoadAsync(id);
        }

        using var response = await api.SendJsonAsync(HttpMethod.Post, $"/api/v1/admin/contacts/{id}/reply", token, new { body = Body.Trim() });
        if (response?.IsSuccessStatusCode != true)
        {
            Error = "Không gửi được phản hồi. Vui lòng thử lại.";
            return await LoadAsync(id);
        }
        return RedirectToPage("/LienHeChiTiet", new { id });
    }

    private async Task<IActionResult> LoadAsync(Guid id)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = Has("cms.update");
        var envelope = await api.GetJsonAsync<ItemEnvelope<AdminThread>>($"/api/v1/admin/contacts/{id}/thread", token);
        Thread = envelope?.Item;
        if (Thread is null) Error ??= "Không tìm thấy phản ánh.";
        return Page();
    }

    private bool HasView() => Has("cms.view") || Has("cms.update");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed record AdminThread(ContactItem Item, IReadOnlyList<ContactMessage> Messages);
    public sealed record ContactItem(Guid Id, string Name, string Phone, string Title, string Body, string Status, DateTimeOffset CreatedAt);
    public sealed record ContactMessage(Guid Id, Guid ContactId, string Sender, string Body, DateTimeOffset CreatedAt);
    private sealed record ItemEnvelope<T>(T? Item);
}
