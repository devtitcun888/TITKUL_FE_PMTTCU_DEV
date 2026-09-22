using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TITKUL.PMTTCU.Web.ApiClients;

public sealed class BackendApiClient
{
    public BackendApiClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    public HttpClient HttpClient { get; }

    public async Task<string?> LoginAsync(string username, string password)
    {
        using var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return string.IsNullOrWhiteSpace(body?.Token) ? null : body.Token;
    }

    public async Task<bool> HasSessionAsync(string token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await HttpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    private sealed record LoginResponse(string? Token);
}
