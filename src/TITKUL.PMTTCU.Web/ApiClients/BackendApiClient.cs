namespace TITKUL.PMTTCU.Web.ApiClients;

public sealed class BackendApiClient
{
    public BackendApiClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    public HttpClient HttpClient { get; }
}
