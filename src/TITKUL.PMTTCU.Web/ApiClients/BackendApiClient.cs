using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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

    public async Task<StaffProfile?> GetProfileAsync(string token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<StaffProfile>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    public async Task<bool> HasSessionAsync(string token) => await GetProfileAsync(token) is not null;

    private sealed record LoginResponse(string? Token);
}

public sealed record StaffProfile(IReadOnlyList<string>? Roles, IReadOnlyList<string>? Permissions);
