using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class DangKyLopModel : PageModel
{
    private readonly BackendApiClient _api;
    public DangKyLopModel(BackendApiClient api) => _api = api;
    public PublicClass? Class { get; private set; }
    public IReadOnlyList<Hamlet> Hamlets { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SubmissionMessage { get; private set; }
    public bool SubmissionAcceptedWithoutCode { get; private set; }
    public bool HamletCatalogUnavailable { get; private set; }

    [BindProperty] public string FullName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string CitizenId { get; set; } = "";
    [BindProperty] public DateOnly? BirthDate { get; set; }
    [BindProperty] public string Gender { get; set; } = "NAM";
    [BindProperty] public Guid? HamletId { get; set; }
    [BindProperty] public string? HamletName { get; set; }
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public bool Consent { get; set; }
    [BindProperty] public string? ReminderEmail { get; set; }
    [BindProperty] public bool ReminderSms { get; set; }
    [BindProperty] public bool ReminderEmailOptIn { get; set; }
    [BindProperty] public bool ReminderZalo { get; set; }
    [BindProperty] public string? Website { get; set; }
    [BindProperty] public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");

    public async Task<IActionResult> OnGetAsync(string ma)
    {
        PublicPrivatePageHeaders.Apply(Response);
        return await LoadAsync(ma);
    }

    public async Task<IActionResult> OnPostAsync(string ma)
    {
        PublicPrivatePageHeaders.Apply(Response);
        var loaded = await LoadAsync(ma);
        if (Class is null || !Class.CanRegister) return loaded;
        using var response = await _api.PostPublicJsonAsync($"/api/v1/public/classes/{Uri.EscapeDataString(ma)}/registrations", new
        {
            fullName = FullName,
            phone = Phone,
            citizenId = CitizenId,
            birthDate = BirthDate,
            gender = Gender,
            hamletId = HamletId,
            hamletName = HamletName,
            note = Note,
            consent = Consent,
            reminderEmail = ReminderEmail,
            reminderSms = ReminderSms,
            reminderEmailOptIn = ReminderEmailOptIn,
            reminderZalo = ReminderZalo,
            website = Website
        }, IdempotencyKey);
        if (response is null)
        {
            ErrorMessage = "Không gửi được đăng ký. Thử lại sau.";
            return Page();
        }

        if (response.IsSuccessStatusCode)
        {
            ItemEnvelope<Result>? body = null;
            try
            {
                body = await response.Content.ReadFromJsonAsync<ItemEnvelope<Result>>();
            }
            catch (JsonException)
            {
                // The API accepted the registration; do not tell the learner to submit it again.
            }
            var code = body?.Item?.Code;
            if (!string.IsNullOrWhiteSpace(code))
            {
                return Redirect($"/dang-ky/{ma}/cam-on?code={Uri.EscapeDataString(code)}");
            }

            SubmissionAcceptedWithoutCode = true;
            SubmissionMessage = "Đăng ký đã được tiếp nhận nhưng chưa lấy được mã đăng ký. Vui lòng liên hệ Trung tâm để xác nhận; tránh gửi lại biểu mẫu.";
            return Page();
        }

        try
        {
            var err = await response.Content.ReadFromJsonAsync<ApiErr>();
            ErrorMessage = err?.Code switch
            {
                "EDU_FULL" => "Lớp đã đủ chỗ. Không nhận thêm đăng ký.",
                "REG_DUPLICATE" => "Số điện thoại hoặc CCCD này đã đăng ký lớp này.",
                "REG_HAMLET" => "Chọn thôn/ấp trong danh mục hoặc nhập tên thôn/ấp.",
                "EDU_CLOSED" or "EDU_WINDOW" => "Lớp chưa mở, đã đóng hoặc hết hạn đăng ký.",
                "REG_IDEMPOTENCY" => "Phiên gửi bị gián đoạn. Tải lại trang rồi gửi lại.",
                "REG_RATE" => "Bạn gửi quá nhanh. Thử lại sau.",
                "REG_REMINDER_EMAIL" => "Vui lòng nhập email hợp lệ nếu chọn nhận nhắc qua email.",
                "REG_CONSENT" => "Bạn cần đồng ý cho Trung tâm sử dụng dữ liệu để quản lý lớp học.",
                _ => err?.Message ?? "Không đăng ký được."
            };
        }
        catch (Exception)
        {
            ErrorMessage = "Không đăng ký được.";
        }

        return Page();
    }

    private async Task<IActionResult> LoadAsync(string ma)
    {
        var classTask = _api.GetPublicJsonResultAsync<ItemEnvelope<PublicClass>>($"/api/v1/public/classes/{Uri.EscapeDataString(ma)}");
        var hamletsTask = _api.GetPublicJsonResultAsync<ListEnvelope<Hamlet>>("/api/v1/public/hamlets");
        await Task.WhenAll(classTask, hamletsTask);
        var cls = await classTask;
        var hamlets = await hamletsTask;
        Class = cls.Value?.Item;
        HamletCatalogUnavailable = !hamlets.IsAvailable || hamlets.Value is null;
        Hamlets = hamlets.Value?.Items ?? [];
        if (Class is null)
        {
            ErrorMessage ??= cls.IsNotFound || cls.IsAvailable
                ? "Không tìm thấy lớp."
                : "Chưa thể tải thông tin lớp. Vui lòng thử lại sau.";
            Response.StatusCode = cls.IsNotFound || cls.IsAvailable
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status503ServiceUnavailable;
        }
        return Page();
    }

    public sealed record PublicClass(string Code, string Name, string ProgramName, DateOnly StartDate, DateOnly EndDate, int Remaining, bool CanRegister, string? ClosedReason);
    public sealed record Hamlet(Guid Id, string Code, string Name);
    private sealed record ItemEnvelope<T>(T? Item);
    private sealed record ListEnvelope<T>(IReadOnlyList<T>? Items);
    private sealed record Result(string Code, string ClassCode);
    private sealed record ApiErr(string? Code, string? Message);
}
