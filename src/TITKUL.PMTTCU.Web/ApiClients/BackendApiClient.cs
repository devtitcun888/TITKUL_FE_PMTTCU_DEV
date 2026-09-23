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
        HttpResponseMessage response;
        try
        {
            response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }

        using (response)
        {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return string.IsNullOrWhiteSpace(body?.Token) ? null : body.Token;
        }
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

    public async Task<T?> GetJsonAsync<T>(string path, string token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions());
        }
        catch (HttpRequestException)
        {
            return default;
        }
        catch (TaskCanceledException)
        {
            return default;
        }
    }

    public async Task<T?> GetPublicJsonAsync<T>(string path)
    {
        try
        {
            using var response = await HttpClient.GetAsync(path);
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions());
        }
        catch (HttpRequestException)
        {
            return default;
        }
        catch (TaskCanceledException)
        {
            return default;
        }
    }

    public async Task<HttpResponseMessage?> PostPublicJsonAsync(string path, object body, string? idempotencyKey = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            }

            return await HttpClient.SendAsync(request);
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

    public async Task<(byte[]? Bytes, string? QrUrl)> GetQrAsync(string path, string token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return (null, null);
            var url = response.Headers.TryGetValues("X-Qr-Url", out var values) ? values.FirstOrDefault() : null;
            return (await response.Content.ReadAsByteArrayAsync(), url);
        }
        catch (HttpRequestException)
        {
            return (null, null);
        }
        catch (TaskCanceledException)
        {
            return (null, null);
        }
    }

    public async Task<HttpResponseMessage?> PostMultipartAsync(string path, string token, MultipartFormDataContent content)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await HttpClient.SendAsync(request);
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

    public async Task<(byte[]? Bytes, string? ContentType, string? FileName)> GetFileAsync(string path, string? token = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return (null, null, null);
            var name = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            return (await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType?.MediaType, name);
        }
        catch (HttpRequestException)
        {
            return (null, null, null);
        }
        catch (TaskCanceledException)
        {
            return (null, null, null);
        }
    }

    public async Task<HttpResponseMessage?> SendJsonAsync(HttpMethod method, string path, string token, object body)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await HttpClient.SendAsync(request);
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

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

    private sealed record LoginResponse(string? Token);
}

public sealed record StaffProfile(IReadOnlyList<string>? Roles, IReadOnlyList<string>? Permissions);
