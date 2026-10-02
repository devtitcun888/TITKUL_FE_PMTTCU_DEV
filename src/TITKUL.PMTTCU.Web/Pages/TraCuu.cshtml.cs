using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class TraCuuModel : PageModel
{
    private readonly BackendApiClient _api;
    public TraCuuModel(BackendApiClient api) => _api = api;
    public Certificate? Item { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty] public string Code { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(string? ma)
    {
        if (string.IsNullOrWhiteSpace(ma)) return Page();
        SetPrivateCacheHeaders();
        Code = ma.Trim();
        await LoadAsync(Code);
        return Page();
    }

    public IActionResult OnPost()
    {
        SetPrivateCacheHeaders();
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "Nhập mã tra cứu.";
            return Page();
        }

        return Redirect("/tra-cuu/" + Uri.EscapeDataString(Code.Trim()));
    }

    public async Task<IActionResult> OnGetPdfAsync(string ma)
    {
        SetPrivateCacheHeaders();
        var file = await _api.GetFileResultAsync($"/api/v1/public/certificates/{Uri.EscapeDataString(ma)}/pdf");
        return HostFile.Download(file, "chung-nhan.pdf", "application/pdf");
    }

    private async Task LoadAsync(string code)
    {
        var result = await _api.GetPublicJsonResultAsync<ItemEnvelope>($"/api/v1/public/certificates/{Uri.EscapeDataString(code)}");
        Item = result.Value?.Item;
        if (Item is null)
        {
            var notFound = result.IsNotFound || result.IsAvailable;
            ErrorMessage = notFound
                ? "Không tìm thấy mã này. Kiểm tra mã và thử lại."
                : "Chưa thể tra cứu lúc này. Vui lòng thử lại sau.";
            Response.StatusCode = notFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status503ServiceUnavailable;
        }
    }

    private void SetPrivateCacheHeaders()
    {
        PublicPrivatePageHeaders.Apply(Response);
    }

    public sealed record Certificate(string LookupCode, string LearnerName, string ClassName, string ClassCode, DateOnly IssuedOn, decimal Percent, string Status);
    private sealed record ItemEnvelope(Certificate? Item);
}
