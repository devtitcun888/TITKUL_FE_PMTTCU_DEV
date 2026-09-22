using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.ApiClients;

public sealed class CorrelationForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.Items[CorrelationMiddleware.ItemKey] as string;
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            request.Headers.Remove(CorrelationMiddleware.HeaderName);
            request.Headers.TryAddWithoutValidation(CorrelationMiddleware.HeaderName, traceId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
