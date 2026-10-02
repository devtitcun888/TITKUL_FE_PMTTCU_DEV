using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed class CmsMenuModel(BackendApiClient api, PublicNavigationProvider publicNavigation) : PageModel
{
    private static readonly string[] AllowedPaths = ["/", "/su-kien", "/cau-lac-bo", "/khao-sat", "/tin-tuc", "/thong-bao", "/binh-dan-hoc-vu-so", "/khoa-hoc-nghe", "/hoc-lieu", "/thu-vien", "/van-ban", "/bieu-mau", "/tra-cuu", "/gioi-thieu", "/co-cau", "/van-hoa-the-thao", "/lien-he", "/lop-hoc"];
    private static readonly string[] AllowedParents = ["surveys", "news", "learning", "center"];
    [BindProperty] public List<MenuItemInput> Items { get; set; } = [];
    [BindProperty] public long ExpectedDraftVersion { get; set; }
    public long PublishedVersion { get; private set; }
    public bool CanEdit { get; private set; }
    public IReadOnlyList<HistoryItem> History { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public static IReadOnlyList<string> Paths => AllowedPaths;
    public static IReadOnlyList<string> Parents => AllowedParents;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Has("cms.navigation.manage")) return Redirect("/admin/khong-quyen");
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveDraftAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        CanEdit = true;
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        if (Items.Count == 0 || Items.Any(item => item.Label is null || item.Label.Trim().Length is < 1 or > 60 || item.Sort is < 0 or > 1000 || (item.Path is not null && !AllowedPaths.Contains(item.Path, StringComparer.Ordinal)) || (item.ParentKey is not null && item.ParentKey != "__cta" && !AllowedParents.Contains(item.ParentKey, StringComparer.Ordinal))))
        {
            ErrorMessage = "Kiểm tra nhãn, thứ tự và chọn đường dẫn trong danh sách được hỗ trợ.";
            await LoadAsync();
            return Page();
        }

        var payload = Items.Select(item => new NavigationItem(item.Key, item.Label.Trim(), item.Path, item.ParentKey, item.Icon, item.Sort, item.Visible)).ToArray();
        using var response = await api.SendJsonAsync(HttpMethod.Put, "/api/v1/admin/navigation/draft", token, new { expectedDraftVersion = ExpectedDraftVersion, items = payload });
        if (response?.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            ErrorMessage = "Bản nháp đã được cập nhật ở phiên khác. Tải lại để lấy phiên bản mới trước khi sửa tiếp.";
            await LoadAsync();
            return Page();
        }
        if (response is null || !response.IsSuccessStatusCode)
        {
            ErrorMessage = "Không lưu được bản nháp. Kiểm tra cấu trúc nhóm và đường dẫn.";
            await LoadAsync();
            return Page();
        }
        return await LoadAsync("Đã lưu bản nháp. Menu công khai chưa đổi cho đến khi bạn công bố.");
    }

    public async Task<IActionResult> OnPostPublishAsync()
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/navigation/publish", token, new { expectedDraftVersion = ExpectedDraftVersion });
        if (response?.StatusCode == System.Net.HttpStatusCode.Conflict) ErrorMessage = "Bản nháp đã thay đổi. Tải lại trước khi công bố.";
        else if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không công bố được menu. Hãy lưu bản nháp hợp lệ trước.";
        else
        {
            publicNavigation.Invalidate();
            return await LoadAsync("Đã công bố menu công khai.");
        }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRestoreAsync(long version)
    {
        if (!CanManage()) return Redirect("/admin/khong-quyen");
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        using var response = await api.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/navigation/restore", token, new { version, expectedDraftVersion = ExpectedDraftVersion });
        if (response?.StatusCode == System.Net.HttpStatusCode.Conflict) ErrorMessage = "Bản nháp đã thay đổi ở phiên khác. Tải lại trang.";
        else if (response is null || !response.IsSuccessStatusCode) ErrorMessage = "Không tìm thấy phiên bản hoặc không khôi phục được bản nháp.";
        else return await LoadAsync($"Đã chép phiên bản {version} vào bản nháp. Hãy xem trước rồi công bố khi phù hợp.");
        await LoadAsync();
        return Page();
    }

    private async Task<IActionResult> LoadAsync(string? success = null)
    {
        var token = Token();
        if (token is null) return Redirect("/admin/dang-nhap");
        CanEdit = CanManage();
        SuccessMessage = success;
        var response = await api.GetJsonAsync<NavigationEnvelope>("/api/v1/admin/navigation", token);
        if (response is null)
        {
            ErrorMessage ??= "Chưa tải được menu từ máy chủ.";
            return Page();
        }
        PublishedVersion = response.Version;
        ExpectedDraftVersion = response.DraftVersion;
        Items = response.Items?.Select(item => new MenuItemInput
        {
            Key = item.Key, Label = item.Label, Path = item.Path, ParentKey = item.ParentKey, Icon = item.Icon, Sort = item.Sort, Visible = item.Visible
        }).ToList() ?? [];
        History = response.History ?? [];
        return Page();
    }

    private bool CanManage() => Has("cms.navigation.manage");
    private bool Has(string permission) => (HttpContext.Items["StaffProfile"] as StaffProfile)?.Permissions?.Contains(permission) == true;
    private string? Token() => Request.Cookies[AdminGateMiddleware.CookieName];

    public sealed class MenuItemInput
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string? Path { get; set; }
        public string? ParentKey { get; set; }
        public string? Icon { get; set; }
        public int Sort { get; set; }
        public bool Visible { get; set; }
    }
    public sealed record NavigationItem(string Key, string Label, string? Path, string? ParentKey, string? Icon, int Sort, bool Visible);
    public sealed record HistoryItem(long Version, DateTime PublishedAt, Guid? PublishedBy);
    private sealed record NavigationEnvelope(long Version, long DraftVersion, IReadOnlyList<NavigationItem>? Items, IReadOnlyList<NavigationItem>? PublishedItems, IReadOnlyList<HistoryItem>? History);
}
