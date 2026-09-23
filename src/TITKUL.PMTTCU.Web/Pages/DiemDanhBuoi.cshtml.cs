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
        await LoadAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        await LoadAsync(id);
        if (Session is not { CanCheckIn: true })
        {
            ErrorMessage = "Buổi chưa mở hoặc đã hết cửa sổ tự điểm danh. Nhờ giảng viên chấm tay.";
            return Page();
        }

        var response = await _api.PostPublicJsonAsync($"/api/v1/public/sessions/{id}/check-in", new
        {
            phone = Phone,
            pin = Pin,
            website = Website
        });
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = response is { StatusCode: System.Net.HttpStatusCode.Conflict }
                ? "Sai mã buổi, SĐT chưa đăng ký lớp này, hoặc ngoài giờ."
                : "Không điểm danh được. Kiểm tra SĐT, mã 6 số và thử lại.";
            return Page();
        }

        var body = await response.Content.ReadFromJsonAsync<ResultEnvelope>();
        SuccessName = body?.Item?.FullName ?? "Bạn";
        return Page();
    }

    private async Task LoadAsync(Guid id)
    {
        var body = await _api.GetPublicJsonAsync<ItemEnvelope>($"/api/v1/public/sessions/{id}/check-in");
        Session = body?.Item;
        if (Session is null) ErrorMessage = "Không tìm thấy buổi học.";
    }

    public sealed record Info(Guid SessionId, string ClassName, string SessionTitle, DateTimeOffset StartAt, DateTimeOffset EndAt, bool CanCheckIn, string? Reason);
    private sealed record ItemEnvelope(Info? Item);
    private sealed record ResultEnvelope(Result? Item);
    private sealed record Result(string FullName, string Status);
}
