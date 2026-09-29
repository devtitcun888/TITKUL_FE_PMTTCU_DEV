using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public sealed class TimKiemModel : PageModel
{
    public static IReadOnlyList<SearchPreferenceOption> PreferenceOptions { get; } =
    [
        new("/tin-tuc", "Tin tức"),
        new("/thong-bao", "Thông báo"),
        new("/su-kien", "Sự kiện"),
        new("/van-ban", "Văn bản"),
        new("/bieu-mau", "Biểu mẫu"),
        new("/thu-vien?loai=IMAGE", "Hình ảnh"),
        new("/thu-vien?loai=VIDEO_LINK", "Video"),
        new("/hoc-lieu", "Học liệu"),
        new("/binh-dan-hoc-vu-so", "Bình dân học vụ số"),
        new("/khoa-hoc-nghe", "Khóa học nghề"),
        new("/lop-hoc", "Lớp học"),
        new("/khao-sat", "Khảo sát")
    ];

    private readonly BackendApiClient _api;

    public TimKiemModel(BackendApiClient api) => _api = api;

    public string Query { get; private set; } = "";
    public IReadOnlyList<SearchSection> Sections { get; private set; } = [];
    public int Total { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? q)
    {
        Query = q?.Trim() ?? "";
        if (Query.Length == 0) return Page();
        if (Query.Length is < 2 or > 80)
        {
            ErrorMessage = "Từ khóa cần có từ 2 đến 80 ký tự.";
            return Page();
        }

        var path = "/api/v1/public/search?q=" + Uri.EscapeDataString(Query);
        var result = await _api.GetPublicJsonAsync<SearchEnvelope>(path);
        if (result is null)
        {
            ErrorMessage = "Chưa thể tải kết quả tìm kiếm. Vui lòng thử lại sau.";
            return Page();
        }

        Sections = result.Sections ?? [];
        Total = result.Total;
        return Page();
    }

    public sealed record SearchItem(string Title, string? Summary, string Url, DateTimeOffset? PublishedAt);
    public sealed record SearchPreferenceOption(string Key, string Title);
    public sealed record SearchSection(string Title, string ListUrl, int Total, IReadOnlyList<SearchItem> Items)
    {
        public string MoreUrl(string query) => ListUrl + (ListUrl.Contains('?') ? "&" : "?") + "q=" + Uri.EscapeDataString(query);
    }
    public sealed record SearchEnvelope(string? Query, IReadOnlyList<SearchSection>? Sections, int Total);
}
