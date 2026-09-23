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
    public async Task Admin_area_requires_login()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/dang-nhap", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_form_has_antiforgery_and_rejects_post_without_it()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var page = await client.GetAsync("/admin/dang-nhap");
        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Tên đăng nhập", html, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);

        var posted = await client.PostAsync("/admin/dang-nhap", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "super.admin",
            ["Password"] = "mat-khau-1"
        }));
        Assert.Equal(HttpStatusCode.BadRequest, posted.StatusCode);
    }

    [Fact]
    public async Task Education_pages_require_login()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        foreach (var path in new[] { "/admin/chuong-trinh", "/admin/doi-tuong", "/admin/thon-ap", "/admin/phong-hoc", "/admin/lop-hoc", "/admin/lich-hoc", "/admin/lop-hoc/" + Guid.Empty, "/admin/hoc-vien", "/admin/hoc-vien/" + Guid.Empty, "/admin/diem-danh/" + Guid.Empty })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/admin/dang-nhap", response.Headers.Location?.OriginalString);
        }
    }

    [Fact]
    public async Task Public_class_pages_render()
    {
        using var client = _factory.CreateClient();
        foreach (var path in new[] { "/lop-hoc", "/lop-hoc/ABC12", "/dang-ky/ABC12", "/dang-ky/ABC12/cam-on", "/dang-ky/ABC12/cam-on?code=DKTESTCODE12ABCD" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
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
