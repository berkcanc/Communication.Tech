using System.Text;
using System.Text.Json;
using communication_tech.Models;
using communication_tech.Services;
using Microsoft.AspNetCore.Mvc;

namespace communication_tech.Controllers;

[ApiController]
[Route("[controller]")]
public class GraphQLController : ControllerBase
{
    private readonly HttpClientService _httpClientService;

    public GraphQLController(HttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
    }

    [HttpGet(Name = "GetBooks")]
    public Task<object> GetBooks()
    {
        var requestBody = new
        {
            query = @"query { books { title author } }"
        };
        return _httpClientService.PostAsync<object, object>("graphql", requestBody, Constants.HTTPBaseAddress);
    }
}