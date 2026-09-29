using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class TheoDoiPhanAnhModel : PageModel
{
    private readonly BackendApiClient _api;
    public TheoDoiPhanAnhModel(BackendApiClient api) => _api = api;
    public string TrackingCode { get; private set; } = "";
    public ContactThread? Thread { get; private set; }
    public string? Error { get; private set; }
    [BindProperty] public string Body { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(string ma) => await LoadAsync(ma);

    public async Task<IActionResult> OnPostAsync(string ma)
    {
        PublicPrivatePageHeaders.Apply(Response);
        using var response = await _api.PostPublicJsonAsync($"/api/v1/public/contacts/{Uri.EscapeDataString(ma)}/messages", new { body = Body });
        if (response?.IsSuccessStatusCode != true)
        {
            Error = "Không gửi được phản hồi. Kiểm tra mã theo dõi và thử lại.";
            return await LoadAsync(ma);
        }
        return RedirectToPage("/TheoDoiPhanAnh", new { ma });
    }

    private async Task<IActionResult> LoadAsync(string code)
    {
        PublicPrivatePageHeaders.Apply(Response);
        TrackingCode = code;
        var result = await _api.GetPublicJsonResultAsync<ItemEnvelope<ContactThread>>($"/api/v1/public/contacts/{Uri.EscapeDataString(code)}");
        Thread = result.Value?.Item;
        if (Thread is null)
        {
            Error ??= result.IsNotFound || result.IsAvailable
                ? "Không tìm thấy trao đổi. Mã có thể sai hoặc phản ánh không hỗ trợ theo dõi trực tuyến."
                : "Chưa thể tải trao đổi lúc này. Vui lòng thử lại sau.";
            Response.StatusCode = result.IsNotFound || result.IsAvailable
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status503ServiceUnavailable;
        }
        return Page();
    }

    public sealed record ContactThread(string Title, string Status, IReadOnlyList<ContactMessage> Messages);
    public sealed record ContactMessage(string Sender, string Body, DateTimeOffset CreatedAt);
    private sealed record ItemEnvelope<T>(T? Item);
}
