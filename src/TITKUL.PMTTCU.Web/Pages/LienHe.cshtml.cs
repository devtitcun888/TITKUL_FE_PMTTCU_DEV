using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class LienHeModel : PageModel
{
    private readonly BackendApiClient _api;
    public LienHeModel(BackendApiClient api) => _api = api;
    public string Name { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string Hotline { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? MapsUrl { get; private set; }
    public string? Message { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty] public string HoTen { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string? Mail { get; set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string Body { get; set; } = "";
    [BindProperty] public string? Website { get; set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var response = await _api.PostPublicJsonAsync("/api/v1/public/contacts", new
        {
            name = HoTen,
            phone = Phone,
            email = Mail,
            title = Title,
            body = Body,
            website = Website
        });
        if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không gửi được. Kiểm tra nội dung hoặc thử lại sau.";
        else Message = "Đã ghi nhận phản ánh.";
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        var config = await _api.GetPublicJsonAsync<GioiThieuModel.ConfigEnvelope>("/api/v1/public/site-config");
        Name = Read(config, "org.name");
        Address = Read(config, "org.address");
        Hotline = Read(config, "org.hotline");
        Email = Read(config, "org.email");
        var maps = Read(config, "maps.url");
        MapsUrl = maps.StartsWith("https://", StringComparison.Ordinal) ? maps : null;
    }

    private static string Read(GioiThieuModel.ConfigEnvelope? config, string key) =>
        config?.Item is not null && config.Item.TryGetValue(key, out var value) ? value : "";
}
