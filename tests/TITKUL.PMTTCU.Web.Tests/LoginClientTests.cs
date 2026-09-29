using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Tests;

public sealed class LoginClientTests
{
    [Fact]
    public async Task LoginAsync_returns_empty_result_when_backend_host_is_unknown()
    {
        using var http = new HttpClient(new FailingHandler())
        {
            BaseAddress = new Uri("https://be.invalid")
        };
        var api = new BackendApiClient(http);

        var result = await api.LoginAsync("admin", "Pmttcu-test-1");

        Assert.NotNull(result);
        Assert.Null(result.Token);
        Assert.Null(result.ErrorCode);
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("No such host is known. (be.invalid:443)");
    }
}
