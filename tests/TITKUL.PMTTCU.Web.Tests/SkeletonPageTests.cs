using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Tests;

public sealed class SkeletonPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SkeletonPageTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_area_returns_skeleton()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/admin");
        var html = await response.Content.ReadAsStringAsync();
        var text = WebUtility.HtmlDecode(html);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Khu vực quản trị", text, StringComparison.Ordinal);
        Assert.Contains("Cms", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_page_shows_trace_id_from_query()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/Error?traceId=abc-12345");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("abc-12345", html, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.GetValues("X-Correlation-Id").Single()));
    }

    [Fact]
    public async Task Api_client_forwards_correlation_id()
    {
        var inner = new RecordingHandler();
        var context = new DefaultHttpContext();
        context.Items[CorrelationMiddleware.ItemKey] = "abc-12345";
        var handler = new CorrelationForwardingHandler(new HttpContextAccessor { HttpContext = context })
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        await client.GetAsync("https://be.invalid/api/v1/system/ping");

        Assert.Equal("abc-12345", inner.Request?.Headers.GetValues(CorrelationMiddleware.HeaderName).Single());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
