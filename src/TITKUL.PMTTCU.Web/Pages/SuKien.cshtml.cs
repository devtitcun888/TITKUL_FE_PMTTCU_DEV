using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class SuKienModel : PageModel
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private readonly BackendApiClient _api;

    public SuKienModel(BackendApiClient api) => _api = api;
    public IReadOnlyList<ActivityItem> Items { get; private set; } = [];
    public DateOnly From { get; private set; }
    public DateOnly To { get; private set; }
    public string? Area { get; private set; }
    public int Total { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(DateOnly? from, DateOnly? to, string? area)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(VietnamOffset).DateTime);
        From = from ?? today;
        To = to ?? From.AddDays(89);
        Area = area?.Trim();
        if (To < From || To.DayNumber - From.DayNumber > 180)
        {
            ErrorMessage = "Chọn ngày kết thúc không trước ngày bắt đầu và giới hạn khoảng tìm kiếm trong 181 ngày.";
            return;
        }
        if (Area?.Length > 100)
        {
            ErrorMessage = "Địa điểm tìm kiếm tối đa 100 ký tự.";
            return;
        }

        var query = new List<string>
        {
            "from=" + Uri.EscapeDataString(From.ToString("yyyy-MM-dd")),
            "to=" + Uri.EscapeDataString(To.ToString("yyyy-MM-dd")),
            "limit=100"
        };
        if (!string.IsNullOrWhiteSpace(Area)) query.Add("area=" + Uri.EscapeDataString(Area));
        var result = await _api.GetPublicJsonAsync<ActivityEnvelope>("/api/v1/public/activities?" + string.Join("&", query));
        if (result is null)
        {
            ErrorMessage = "Chưa thể tải lịch hoạt động. Vui lòng thử lại sau.";
            return;
        }

        Items = result.Items ?? [];
        Total = result.Total;
    }

    public static string ActivityHref(ActivityItem item) => item.Kind == "CLASS_SESSION" && !string.IsNullOrWhiteSpace(item.ClassCode)
        ? "/lop-hoc/" + Uri.EscapeDataString(item.ClassCode)
        : "/su-kien/" + Uri.EscapeDataString(item.EventSlug ?? "");

    public static string RegistrationMessage(ActivityItem item) => item.CanRegister == true
        ? $"Đang nhận đăng ký · còn {item.Remaining ?? 0} chỗ"
        : item.RegistrationState switch
        {
            "EDU_FULL" => "Đã đủ chỗ",
            "EDU_WINDOW" => "Ngoài thời hạn đăng ký",
            _ => "Lớp hiện chưa nhận đăng ký"
        };

    public sealed record ActivityItem(
        string Kind,
        string Title,
        string? EventSlug,
        string? ClassCode,
        DateTimeOffset StartAt,
        DateTimeOffset EndAt,
        string? Location,
        string? Summary,
        bool? CanRegister,
        string? RegistrationState,
        int? Remaining,
        DateTimeOffset? RegistrationClosesAt);
    private sealed record ActivityEnvelope(IReadOnlyList<ActivityItem>? Items, DateOnly? From, DateOnly? To, int Total);
}
