using Microsoft.Extensions.Caching.Memory;

namespace TITKUL.PMTTCU.Web.ApiClients;

public sealed class PublicUrgentNoticeProvider
{
    private const string CacheKey = "public-urgent-notice";
    private readonly IMemoryCache _cache;
    private readonly BackendApiClient _api;
    private PortalNavigationData? _requestValue;
    private bool _hasRequestValue;

    public PublicUrgentNoticeProvider(IMemoryCache cache, BackendApiClient api)
    {
        _cache = cache;
        _api = api;
    }

    public void SetForCurrentRequest(
        string? title,
        string? slug,
        IReadOnlyList<SurveyLink>? surveys = null)
    {
        var urgent = string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(slug)
            ? null
            : new UrgentNotice(title, slug);
        _requestValue = new PortalNavigationData(urgent, surveys ?? []);
        _hasRequestValue = true;
        _cache.Set(CacheKey, new CachedNotice(_requestValue), TimeSpan.FromMinutes(1));
    }

    public async Task<PortalNavigationData> GetAsync()
    {
        if (_hasRequestValue) return _requestValue!;
        if (_cache.TryGetValue(CacheKey, out CachedNotice? cached) && cached is not null)
        {
            return cached.Data;
        }

        var response = await _api.GetPublicJsonAsync<HomeEnvelope>("/api/v1/public/home");
        var home = response?.Item;
        var first = home?.Urgent?.FirstOrDefault();
        SetForCurrentRequest(
            first?.Title,
            first?.Slug,
            home?.Surveys?.Select(survey => new SurveyLink(survey.Code, survey.Title, survey.EndAt)).ToArray());
        return _requestValue!;
    }

    public sealed record PortalNavigationData(UrgentNotice? Urgent, IReadOnlyList<SurveyLink> Surveys);
    public sealed record UrgentNotice(string Title, string Slug);
    public sealed record SurveyLink(string Code, string Title, DateTimeOffset EndAt);
    private sealed record CachedNotice(PortalNavigationData Data);
    private sealed record HomeNotice(string Title, string Slug);
    private sealed record HomeSurvey(string Code, string Title, DateTimeOffset EndAt);
    private sealed record HomeBody(IReadOnlyList<HomeNotice>? Urgent, IReadOnlyList<HomeSurvey>? Surveys);
    private sealed record HomeEnvelope(HomeBody? Item);
}
