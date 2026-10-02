using Microsoft.Extensions.Caching.Memory;

namespace TITKUL.PMTTCU.Web.ApiClients;

public sealed class PublicNavigationProvider(BackendApiClient api, IMemoryCache cache)
{
    private const string CacheKey = "public-navigation-v1";

    public async Task<IReadOnlyList<PublicNavigationItem>> GetAsync()
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<PublicNavigationItem>? cached) && cached is not null) return cached;
        var result = await api.GetPublicJsonResultAsync<NavigationEnvelope>("/api/v1/public/navigation");
        var items = result.Value?.Items?.Where(item => item.Visible).ToArray();
        if (items is null || items.Length == 0) items = Defaults.Where(item => item.Visible).ToArray();
        cache.Set(CacheKey, (IReadOnlyList<PublicNavigationItem>)items, TimeSpan.FromMinutes(1));
        return items;
    }

    public void Invalidate() => cache.Remove(CacheKey);

    private sealed record NavigationEnvelope(long Version, IReadOnlyList<PublicNavigationItem>? Items);

    public sealed record PublicNavigationItem(string Key, string Label, string? Path, string? ParentKey, string? Icon, int Sort, bool Visible);

    private static readonly PublicNavigationItem[] Defaults =
    [
        new("home", "Trang chủ", "/", null, "home", 0, true),
        new("schedule", "Lịch hoạt động", "/su-kien", null, "event", 10, true),
        new("clubs", "Câu lạc bộ", "/cau-lac-bo", null, "groups", 20, true),
        new("surveys", "Khảo sát", null, null, "fact_check", 30, true),
        new("all-surveys", "Tất cả khảo sát", "/khao-sat", "surveys", null, 10, true),
        new("news", "Tin & thông báo", null, null, "campaign", 40, true),
        new("news-list", "Tin tức", "/tin-tuc", "news", null, 10, true),
        new("notices", "Thông báo", "/thong-bao", "news", null, 20, true),
        new("learning", "Học liệu", null, null, "menu_book", 50, true),
        new("digital-literacy", "Bình dân học vụ số", "/binh-dan-hoc-vu-so", "learning", null, 10, true),
        new("vocational", "Các khóa học nghề", "/khoa-hoc-nghe", "learning", null, 20, true),
        new("materials", "Học liệu số", "/hoc-lieu", "learning", null, 30, true),
        new("gallery", "Thư viện", "/thu-vien", "learning", null, 40, true),
        new("documents", "Văn bản", "/van-ban", "learning", null, 50, true),
        new("forms", "Biểu mẫu", "/bieu-mau", "learning", null, 60, true),
        new("lookup", "Tra cứu", "/tra-cuu", null, "search", 60, true),
        new("center", "Về Trung tâm", null, null, "account_balance", 70, true),
        new("about", "Giới thiệu", "/gioi-thieu", "center", null, 10, true),
        new("organization", "Cơ cấu tổ chức", "/co-cau", "center", null, 20, true),
        new("culture", "Văn hóa – Thể thao", "/van-hoa-the-thao", "center", null, 30, true),
        new("contact", "Liên hệ", "/lien-he", null, "call", 80, true),
        new("cta-register", "Đăng ký học", "/lop-hoc", "__cta", null, 0, true)
    ];
}
