using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class DiemDanhBuoiModel : PageModel
{
    private readonly BackendApiClient _api;
    public DiemDanhBuoiModel(BackendApiClient api) => _api = api;

    public Info? Session { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessName { get; private set; }

    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string Pin { get; set; } = "";
    [BindProperty] public string? Website { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        PublicPrivatePageHeaders.Apply(Response);
        await LoadAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        PublicPrivatePageHeaders.Apply(Response);
        await LoadAsync(id);
        if (Session is null) return Page();
        if (!Session.CanCheckIn)
        {
            ErrorMessage = "Buổi chưa mở hoặc đã hết cửa sổ tự điểm danh. Nhờ giảng viên chấm tay.";
            return Page();
        }

        using var response = await _api.PostPublicJsonAsync($"/api/v1/public/sessions/{id}/check-in", new
        {
            phone = Phone,
            pin = Pin,
            website = Website
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ApiError? error = null;
            try
            {
                if (response is not null) error = await response.Content.ReadFromJsonAsync<ApiError>();
            }
            catch (JsonException)
            {
                // Keep a safe generic message when the API error payload is malformed.
            }
            ErrorMessage = error?.Code switch
            {
                "REG_RATE" => "Bạn gửi yêu cầu quá nhanh. Vui lòng đợi rồi thử lại.",
                "EDU_NOT_FOUND" => "Không tìm thấy buổi học.",
                "EDU_STATE" => "Buổi học đã bị hủy; nhờ giảng viên hỗ trợ.",
                "ATT_FUTURE" => "Ngoài cửa sổ tự điểm danh; nhờ giảng viên hỗ trợ.",
                "ATT_PIN" => "Mã buổi không đúng. Kiểm tra lại mã 6 số với giảng viên.",
                "ATT_ROSTER" => "Số điện thoại này chưa đăng ký lớp của buổi học.",
                _ => "Không điểm danh được. Kiểm tra số điện thoại, mã 6 số và thử lại."
            };
            return Page();
        }

        try
        {
            var body = await response.Content.ReadFromJsonAsync<ResultEnvelope>();
            SuccessName = body?.Item?.FullName ?? "Bạn";
        }
        catch (JsonException)
        {
            SuccessName = "Bạn";
        }
        return Page();
    }

    private async Task LoadAsync(Guid id)
    {
        var result = await _api.GetPublicJsonResultAsync<ItemEnvelope>($"/api/v1/public/sessions/{id}/check-in");
        Session = result.Value?.Item;
        if (Session is null)
        {
            var notFound = result.IsNotFound || result.IsAvailable;
            ErrorMessage = notFound ? "Không tìm thấy buổi học." : "Chưa thể tải thông tin buổi học. Vui lòng thử lại sau.";
            Response.StatusCode = notFound ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
        }
    }

    public sealed record Info(Guid SessionId, string ClassName, string SessionTitle, DateTimeOffset StartAt, DateTimeOffset EndAt, bool CanCheckIn, string? Reason);
    private sealed record ItemEnvelope(Info? Item);
    private sealed record ResultEnvelope(Result? Item);
    private sealed record Result(string FullName, string Status);
    private sealed record ApiError(string? Code, string? Message);
}
