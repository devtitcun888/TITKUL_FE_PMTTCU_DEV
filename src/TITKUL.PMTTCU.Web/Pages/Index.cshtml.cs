using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Pages;

public class IndexModel : PageModel
{
    private readonly BackendApiClient _api;
    private readonly PublicSiteConfigProvider _siteConfig;
    private readonly PublicUrgentNoticeProvider _urgentNotice;

    public IndexModel(BackendApiClient api, PublicSiteConfigProvider siteConfig, PublicUrgentNoticeProvider urgentNotice)
    {
        _api = api;
        _siteConfig = siteConfig;
        _urgentNotice = urgentNotice;
    }

    public IReadOnlyList<NoticeItem> Urgent { get; private set; } = [];
    public IReadOnlyList<NoticeItem> Notices { get; private set; } = [];
    public IReadOnlyList<PostItem> Pinned { get; private set; } = [];
    public IReadOnlyList<PostItem> Latest { get; private set; } = [];
    public IReadOnlyList<EventItem> Upcoming { get; private set; } = [];
    public IReadOnlyList<ActivityItem> Activities { get; private set; } = [];
    public IReadOnlyList<MaterialItem> Materials { get; private set; } = [];
    public IReadOnlyList<ClassItem> Classes { get; private set; } = [];
    public IReadOnlyList<SurveyItem> Surveys { get; private set; } = [];
    public IReadOnlyList<GalleryItem> Galleries { get; private set; } = [];
    public IReadOnlyList<PostItem> DigitalLiteracyPosts { get; private set; } = [];
    public IReadOnlyList<PostItem> VocationalPosts { get; private set; } = [];
    public IReadOnlyList<ContentCategoryItem> VocationalCategories { get; private set; } = [];
    public PublicVisitStats? VisitStats { get; private set; }
    public IReadOnlyList<DocumentItem> Documents { get; private set; } = [];
    public bool HomeUnavailable { get; private set; }
    public bool ActivitiesUnavailable { get; private set; }
    public bool DocumentsUnavailable { get; private set; }
    public string Address { get; private set; } = "";
    public string Hotline { get; private set; } = "";
    public string? TelephoneHref { get; private set; }
    public string? MapsUrl { get; private set; }
    public string WorkingHours { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var homeTask = _api.GetPublicJsonAsync<ItemEnvelope>("/api/v1/public/home");
        var documentsTask = _api.GetPublicJsonAsync<DocumentEnvelope>("/api/v1/public/documents?pageSize=10");
        var activitiesTask = _api.GetPublicJsonAsync<ActivityEnvelope>("/api/v1/public/activities?limit=10");
        var visitStatsTask = _api.GetPublicJsonAsync<VisitStatsEnvelope>("/api/v1/public/visit-stats");
        await Task.WhenAll(homeTask, documentsTask, activitiesTask, visitStatsTask);
        var home = await homeTask;
        HomeUnavailable = home?.Item is null;
        if (home?.Item is { } item)
        {
            Urgent = item.Urgent ?? [];
            var urgent = Urgent.FirstOrDefault();
            _urgentNotice.SetForCurrentRequest(
                urgent?.Title,
                urgent?.Slug,
                (item.Surveys ?? []).Select(survey => new PublicUrgentNoticeProvider.SurveyLink(survey.Code, survey.Title, survey.EndAt)).ToArray());
            Notices = item.Notices ?? [];
            Pinned = item.Pinned ?? [];
            Latest = item.Latest ?? [];
            Upcoming = item.Upcoming ?? [];
            Materials = item.Materials ?? [];
            Classes = item.Classes ?? [];
            Surveys = item.Surveys ?? [];
            Galleries = item.Galleries ?? [];
            var needsDigitalFallback = item.DigitalLiteracyPosts is null;
            var needsVocationalFallback = item.VocationalPosts is null;
            var categoriesTask = needsVocationalFallback && item.VocationalCategories is null
                ? _api.GetPublicJsonAsync<CategoryEnvelope>("/api/v1/public/categories")
                : Task.FromResult<CategoryEnvelope?>(null);
            var digitalPostsTask = needsDigitalFallback
                ? GetCategoryPostsAsync("binh-dan-hoc-vu-so", 4)
                : Task.FromResult<PostListEnvelope?>(null);
            await Task.WhenAll(categoriesTask, digitalPostsTask);

            DigitalLiteracyPosts = item.DigitalLiteracyPosts ?? digitalPostsTask.Result?.Items ?? [];
            VocationalCategories = item.VocationalCategories ?? (categoriesTask.Result?.Items ?? [])
                .Where(category => category.Active
                    && category.Kind == "TIN_TUC"
                    && category.Slug.StartsWith("khoa-hoc-nghe-", StringComparison.Ordinal))
                .OrderBy(category => category.Sort)
                .ThenBy(category => category.Name, StringComparer.CurrentCulture)
                .Take(8)
                .ToArray();
            if (needsVocationalFallback && VocationalCategories.Count > 0)
            {
                var vocationalFeeds = await Task.WhenAll(VocationalCategories.Select(category =>
                    GetCategoryPostsAsync(category.Slug, 4)));
                VocationalPosts = vocationalFeeds
                    .Where(feed => feed is not null)
                    .SelectMany(feed => feed!.Items ?? [])
                    .DistinctBy(post => post.Slug)
                    .OrderByDescending(post => post.PublishedAt)
                    .ThenBy(post => post.Title, StringComparer.CurrentCulture)
                    .Take(4)
                    .ToArray();
            }
            else
            {
                VocationalPosts = item.VocationalPosts ?? [];
            }
            _siteConfig.SetForCurrentRequest(item.Settings);
        }

        var activities = await activitiesTask;
        ActivitiesUnavailable = activities is null;
        Activities = activities?.Items ?? Upcoming.Select(activity => new ActivityItem(
            "EVENT", activity.Title, activity.Slug, null, activity.StartAt, activity.EndAt, activity.Location, null, null, null, null, null)).ToArray();
        var documents = await documentsTask;
        DocumentsUnavailable = documents is null;
        Documents = documents?.Items ?? [];
        VisitStats = (await visitStatsTask)?.Item;
        var settings = await _siteConfig.GetAsync();
        Address = Setting(settings, "org.address");
        Hotline = Setting(settings, "org.hotline");
        TelephoneHref = string.IsNullOrWhiteSpace(Hotline)
            ? null
            : new string(Hotline.Where(character => char.IsDigit(character) || character == '+').ToArray());
        WorkingHours = Setting(settings, "org.hours");
        var maps = Setting(settings, "maps.url");
        MapsUrl = Uri.TryCreate(maps, UriKind.Absolute, out var mapUri) && mapUri.Scheme == Uri.UriSchemeHttps ? mapUri.AbsoluteUri : null;
    }

    public async Task<IActionResult> OnGetMediaAsync(Guid id)
    {
        var file = await _api.GetFileResultAsync("/api/v1/public/files/media/" + id);
        if (!file.IsSuccess) return file.IsNotFound ? NotFound() : StatusCode(503);
        return File(file.Bytes!, file.ContentType ?? "image/jpeg");
    }

    public static string FormatVisitCount(long count) => count.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    private static string Setting(IReadOnlyDictionary<string, string> settings, string key) =>
        settings.TryGetValue(key, out var value) ? value?.Trim() ?? "" : "";

    private Task<PostListEnvelope?> GetCategoryPostsAsync(string categorySlug, int pageSize) =>
        _api.GetPublicJsonAsync<PostListEnvelope>(
            $"/api/v1/public/posts?page=1&pageSize={pageSize}&categorySlug={Uri.EscapeDataString(categorySlug)}");

    public sealed record NoticeItem(string Title, string Slug, string Level);
    public sealed record PostItem(string Title, string Slug, string? Summary, DateTimeOffset? PublishedAt, string? CoverUrl, string? ThumbnailUrl = null);
    public sealed record EventItem(string Title, string Slug, DateTimeOffset StartAt, DateTimeOffset EndAt, string? Location);
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
    public sealed record MaterialItem(string Title, string Slug, string? Summary);
    public sealed record ClassItem(
        string Code,
        string Name,
        string ProgramName,
        DateOnly StartDate,
        DateOnly EndDate,
        int Capacity,
        int Remaining,
        bool CanRegister,
        string? ClosedReason,
        DateTimeOffset? RegistrationClosesAt = null);
    public sealed record SurveyItem(string Code, string Title, string? Summary, DateTimeOffset EndAt);
    public sealed record GalleryMedia(string Kind, string? Title, string? AltText, string? ExternalUrl, Guid? FileId);
    public sealed record GalleryItem(string Title, string Slug, string? Summary, GalleryMedia? Cover);
    public sealed record ContentCategoryItem(Guid Id, string Kind, string Name, string Slug, string? Description, int Sort, bool Active);
    public sealed record DocumentItem(Guid Id, string? Symbol, string Title, string? Issuer, DateOnly? IssuedOn);
    public sealed record PublicVisitStats(long OnlineNow, long Today, long Yesterday, long ThisWeek, long LastWeek, long ThisMonth, long LastMonth, long Total);
    private sealed record VisitStatsEnvelope(PublicVisitStats? Item);
    private sealed record DocumentEnvelope(IReadOnlyList<DocumentItem>? Items);
    private sealed record ActivityEnvelope(IReadOnlyList<ActivityItem>? Items);
    private sealed record CategoryEnvelope(IReadOnlyList<ContentCategoryItem>? Items);
    private sealed record PostListEnvelope(IReadOnlyList<PostItem>? Items);
    private sealed record HomeBody(
        IReadOnlyList<NoticeItem>? Urgent,
        IReadOnlyList<NoticeItem>? Notices,
        IReadOnlyList<PostItem>? Pinned,
        IReadOnlyList<PostItem>? Latest,
        IReadOnlyList<EventItem>? Upcoming,
        IReadOnlyList<MaterialItem>? Materials,
        IReadOnlyList<ClassItem>? Classes,
        IReadOnlyList<SurveyItem>? Surveys,
        IReadOnlyList<GalleryItem>? Galleries,
        IReadOnlyList<PostItem>? DigitalLiteracyPosts,
        IReadOnlyList<PostItem>? VocationalPosts,
        IReadOnlyList<ContentCategoryItem>? VocationalCategories,
        IReadOnlyDictionary<string, string>? Settings);
    private sealed record ItemEnvelope(HomeBody? Item);
}
