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
        Code = ma.Trim();
        await LoadAsync(Code);
        return Page();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "Nhập mã tra cứu.";
            return Page();
        }

        return Redirect("/tra-cuu/" + Uri.EscapeDataString(Code.Trim()));
    }

    public async Task<IActionResult> OnGetPdfAsync(string ma)
    {
        var file = await _api.GetFileAsync($"/api/v1/public/certificates/{ma}/pdf");
        if (file.Bytes is null) return NotFound();
        return File(file.Bytes, "application/pdf", file.FileName ?? "chung-nhan.pdf");
    }

    private async Task LoadAsync(string code)
    {
        var body = await _api.GetPublicJsonAsync<ItemEnvelope>($"/api/v1/public/certificates/{code}");
        Item = body?.Item;
        if (Item is null) ErrorMessage = "Không tìm thấy mã này.";
    }

    public sealed record Certificate(string LookupCode, string LearnerName, string ClassName, string ClassCode, DateOnly IssuedOn, decimal Percent, string Status);
    private sealed record ItemEnvelope(Certificate? Item);
}
