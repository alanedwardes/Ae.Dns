public sealed class MyTypedClient : IMyTypedClient
{
    private readonly HttpClient _httpClient;

    public MyTypedClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> GetGoogle(CancellationToken token)
    {
        return await _httpClient.GetAsync("https://www.google.com/", token);
    }
}