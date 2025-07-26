
namespace communication_tech.Services;

public class HttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string route, TRequest requestBody, string baseAddress)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUri = new Uri(baseAddress ?? throw new InvalidOperationException("BaseAddress is null"));
        var uriBuilder = new UriBuilder(new Uri(baseUri, route));
        try
        {
            var response = await client.PostAsJsonAsync(uriBuilder.Uri, requestBody);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return default;
    }
}