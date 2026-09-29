using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class HuyDangKyModel : PageModel
{
    private readonly BackendApiClient _api;
    public HuyDangKyModel(BackendApiClient api) => _api = api;
    [BindProperty] public string RegistrationCode { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    public string? Message { get; private set; }
    public string? Error { get; private set; }

    public void OnGet(string? code)
    {
        PublicPrivatePageHeaders.Apply(Response);
        RegistrationCode = code ?? "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        PublicPrivatePageHeaders.Apply(Response);
        using var response = await _api.PostPublicJsonAsync("/api/v1/public/registrations/cancel", new
        {
            registrationCode = RegistrationCode,
            phone = Phone
        });
        if (response?.IsSuccessStatusCode == true)
            Message = "Đã hủy đăng ký. Nếu bạn cần đăng ký lại, hãy chọn lớp còn mở trên cổng thông tin.";
        else if (response?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            Error = "Bạn gửi yêu cầu quá nhanh. Vui lòng thử lại sau.";
        else
            Error = "Không hủy được đăng ký. Hãy kiểm tra mã, số điện thoại và hạn hủy của lớp.";
        return Page();
    }

    public async Task<IActionResult> OnPostDisableRemindersAsync()
    {
        PublicPrivatePageHeaders.Apply(Response);
        using var response = await _api.PostPublicJsonAsync("/api/v1/public/registrations/reminders/disable", new
        {
            registrationCode = RegistrationCode,
            phone = Phone
        });
        if (response?.IsSuccessStatusCode == true)
            Message = "Đã tắt toàn bộ nhắc lịch cho đăng ký này. Đăng ký lớp vẫn được giữ nguyên.";
        else if (response?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            Error = "Bạn gửi yêu cầu quá nhanh. Vui lòng thử lại sau.";
        else
            Error = "Không cập nhật được lựa chọn nhắc lịch. Hãy kiểm tra mã đăng ký và số điện thoại.";
        return Page();
    }
}
