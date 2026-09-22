using System.Text.RegularExpressions;

namespace TITKUL.PMTTCU.Web.Observability;

public sealed class CorrelationMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "TraceId";

    private static readonly Regex SafeId = new("^[A-Za-z0-9._-]{8,64}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        if (context.Items[ItemKey] is not string existing || string.IsNullOrWhiteSpace(existing))
        {
            var incoming = context.Request.Headers[HeaderName].ToString();
            existing = SafeId.IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");
            context.Items[ItemKey] = existing;
        }

        context.Response.Headers[HeaderName] = existing;
        return _next(context);
    }
}
