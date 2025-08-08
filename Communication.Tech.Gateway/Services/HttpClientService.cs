using System.Text.Json;
using System.Web;

namespace communication_tech.Services;

public class HttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<TResponse?> GetAsyncWithQueryString<TResponse>(string baseAddress, string route, Dictionary<string, string> queryParams)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUri = new Uri(baseAddress ?? throw new InvalidOperationException("BaseAddress is null"));
        var uriBuilder = new UriBuilder(new Uri(baseUri, route));

        if (queryParams.Count > 0)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            foreach (var param in queryParams)
            {
                queryString[param.Key] = param.Value;
            }
            uriBuilder.Query = queryString.ToString();
        }
        
        try
        {
            var response = await client.GetAsync(uriBuilder.Uri);
            response.EnsureSuccessStatusCode();
            
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<TResponse>(jsonResponse, options);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return default;
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
                return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return default;
    }
}