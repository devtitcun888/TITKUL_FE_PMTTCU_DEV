using Microsoft.Extensions.Caching.Memory;

namespace TITKUL.PMTTCU.Web.ApiClients;

public sealed class PublicSiteConfigProvider
{
    private const string CacheKey = "public-site-config";
    private readonly IMemoryCache _cache;
    private readonly BackendApiClient _api;
    private IReadOnlyDictionary<string, string>? _requestValue;

    public PublicSiteConfigProvider(IMemoryCache cache, BackendApiClient api)
    {
        _cache = cache;
        _api = api;
    }

    public void SetForCurrentRequest(IReadOnlyDictionary<string, string>? settings)
    {
        if (settings is null) return;
        _requestValue = settings;
        _cache.Set(CacheKey, settings, TimeSpan.FromMinutes(5));
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAsync()
    {
        if (_requestValue is not null) return _requestValue;
        if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, string>? cached) && cached is not null)
        {
            return cached;
        }

        var response = await _api.GetPublicJsonAsync<ConfigEnvelope>("/api/v1/public/site-config");
        var settings = response?.Item ?? new Dictionary<string, string>(StringComparer.Ordinal);
        SetForCurrentRequest(settings);
        return settings;
    }

    private sealed record ConfigEnvelope(IReadOnlyDictionary<string, string>? Item);
}
