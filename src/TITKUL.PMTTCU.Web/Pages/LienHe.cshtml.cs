using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;
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
    public string? MapsLinkUrl => MapsUrl;
    public string? MapsEmbedUrl { get; private set; }
    public string TelephoneHref => new(Hotline.Where(character => char.IsDigit(character) || character == '+').ToArray());
    [TempData] public string? Message { get; set; }
    public string? ErrorMessage { get; private set; }
    [TempData] public string? TrackingCode { get; set; }
    [TempData] public bool TrackingCodeUnavailable { get; set; }
    public bool ConfigurationUnavailable { get; private set; }

    [BindProperty] public string HoTen { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string? Mail { get; set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string Body { get; set; } = "";
    [BindProperty] public string? Website { get; set; }

    public async Task OnGetAsync()
    {
        PublicPrivatePageHeaders.Apply(Response);
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        PublicPrivatePageHeaders.Apply(Response);
        using var response = await _api.PostPublicJsonAsync("/api/v1/public/contacts", new
        {
            name = HoTen,
            phone = Phone,
            email = Mail,
            title = Title,
            body = Body,
            website = Website
        });
        if (response?.IsSuccessStatusCode == true)
        {
            try
            {
                var result = await response.Content.ReadFromJsonAsync<ContactSubmitResponse>();
                TrackingCode = result?.TrackingCode;
            }
            catch (JsonException)
            {
                // The API accepted the contact; keep that state even if its reference payload is malformed.
            }
            TrackingCodeUnavailable = string.IsNullOrWhiteSpace(TrackingCode);
        }
        if (response?.IsSuccessStatusCode == true)
        {
            Message = TrackingCodeUnavailable
                ? "Phản ánh đã được tiếp nhận nhưng chưa lấy được mã theo dõi. Vui lòng liên hệ Trung tâm để xác nhận; tránh gửi lại biểu mẫu."
                : "Đã ghi nhận phản ánh.";
            return RedirectToPage("/LienHe");
        }

        ErrorMessage = "Không gửi được. Kiểm tra nội dung hoặc thử lại sau.";
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        var result = await _api.GetPublicJsonResultAsync<GioiThieuModel.ConfigEnvelope>("/api/v1/public/site-config");
        ConfigurationUnavailable = !result.IsAvailable || result.Value is null;
        var config = result.Value;
        Name = Read(config, "org.name");
        Address = Read(config, "org.address");
        Hotline = Read(config, "org.hotline");
        Email = Read(config, "org.email");
        var maps = Read(config, "maps.url");
        MapsUrl = GoogleMapsUrl.Link(maps, Address);
        MapsEmbedUrl = GoogleMapsUrl.Embed(maps);
    }

    private static string Read(GioiThieuModel.ConfigEnvelope? config, string key) =>
        config?.Item is not null && config.Item.TryGetValue(key, out var value) ? value : "";

    private sealed record ContactSubmitResponse(string? TrackingCode);
}
