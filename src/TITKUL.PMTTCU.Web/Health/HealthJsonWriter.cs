using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TITKUL.PMTTCU.Web.Health;

internal static class HealthJsonWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString()
            })
        });
    }
}
