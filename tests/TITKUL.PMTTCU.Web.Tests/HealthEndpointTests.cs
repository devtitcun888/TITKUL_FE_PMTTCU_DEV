using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TITKUL.PMTTCU.Web.Tests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_returns_healthy_json(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());

        var body = await response.Content.ReadFromJsonAsync<HealthBody>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
        Assert.Contains(body.Checks, check => check.Name == "self" && check.Status == "Healthy");
    }

    [Fact]
    public async Task Home_page_returns_foundation_content()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var text = System.Net.WebUtility.HtmlDecode(html);
        Assert.Contains("Trung tâm cung ứng dịch vụ sự nghiệp công xã Tân Trụ", text, StringComparison.Ordinal);
        Assert.Contains("Cổng thông tin", text, StringComparison.Ordinal);
        Assert.Contains("Ứng dụng cổng thông tin đã sẵn sàng.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Configuration_has_no_secret_and_uses_placeholder_backend()
    {
        using var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        Assert.True(string.IsNullOrEmpty(configuration["Database:Password"]));
        Assert.True(string.IsNullOrEmpty(configuration["ConnectionStrings:Default"]));
        Assert.Equal("https://be.invalid", configuration["Backend:BaseUrl"]);
        Assert.Equal(1_048_576, configuration.GetValue<long>("Http:MaxRequestBodyBytes"));
    }

    private sealed record HealthBody(string Status, IReadOnlyList<HealthCheckBody> Checks);

    private sealed record HealthCheckBody(string Name, string Status);
}
