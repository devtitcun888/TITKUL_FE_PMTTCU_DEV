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

    public async Task<LoginCallResult> LoginAsync(string username, string password, string? otpCode = null)
    {
        HttpResponseMessage response;
        try
        {
            response = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", new { username, password, otpCode });
        }
        catch (HttpRequestException)
        {
            return new LoginCallResult(null, null);
        }
        catch (TaskCanceledException)
        {
            return new LoginCallResult(null, null);
        }

        using (response)
        {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions());
            return new LoginCallResult(null, error?.Code);
        }

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return new LoginCallResult(string.IsNullOrWhiteSpace(body?.Token) ? null : body.Token, null);
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
        catch (JsonException)
        {
            return default;
        }
    }

    public async Task<T?> GetPublicJsonAsync<T>(string path)
    {
        var result = await GetPublicJsonResultAsync<T>(path);
        return result.IsAvailable ? result.Value : default;
    }

    public async Task<PublicJsonResult<T>> GetPublicJsonResultAsync<T>(string path)
    {
        try
        {
            using var response = await HttpClient.GetAsync(path);
            if (!response.IsSuccessStatusCode) return new(default, response.StatusCode, false);
            try
            {
                var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions());
                return new(value, response.StatusCode, false);
            }
            catch (JsonException)
            {
                return new(default, response.StatusCode, true);
            }
        }
        catch (HttpRequestException)
        {
            return new(default, null, true);
        }
        catch (TaskCanceledException)
        {
            return new(default, null, true);
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
        var result = await GetFileResultAsync(path, token);
        return result.IsSuccess
            ? (result.Bytes, result.ContentType, result.FileName)
            : (null, null, null);
    }

    public async Task<PublicFileResult> GetFileResultAsync(string path, string? token = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new(null, null, null, response.StatusCode);
            var name = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return new(bytes, response.Content.Headers.ContentType?.MediaType, name, response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return new(null, null, null, null);
        }
        catch (TaskCanceledException)
        {
            return new(null, null, null, null);
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
    private sealed record ApiErrorResponse(string? Code, string? Message);
}

public sealed record LoginCallResult(string? Token, string? ErrorCode);

public sealed record PublicJsonResult<T>(T? Value, System.Net.HttpStatusCode? StatusCode, bool PayloadInvalid)
{
    public bool IsNotFound => StatusCode == System.Net.HttpStatusCode.NotFound;
    public bool IsAvailable => StatusCode is { } status && (int)status >= 200 && (int)status < 300 && !PayloadInvalid;
}

public sealed record PublicFileResult(byte[]? Bytes, string? ContentType, string? FileName, System.Net.HttpStatusCode? StatusCode)
{
    public bool IsNotFound => StatusCode == System.Net.HttpStatusCode.NotFound;
    public bool IsSuccess => StatusCode is { } status && (int)status >= 200 && (int)status < 300 && Bytes is not null;
}

public sealed record StaffProfile(IReadOnlyList<string>? Roles, IReadOnlyList<string>? Permissions);
