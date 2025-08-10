using System.Text;
using System.Text.Json;
using communication_tech.Models;
using communication_tech.Services;
using Communication.Tech.Protos;
using Microsoft.AspNetCore.Mvc;

namespace communication_tech.Controllers;

[ApiController]
[Route("[controller]")]
public class GraphQLController : ControllerBase
{
    private readonly HttpClientService _httpClientService;
    private readonly IConfiguration _configuration;

    public GraphQLController(HttpClientService httpClientService, IConfiguration configuration)
    {
        _httpClientService = httpClientService;
        _configuration = configuration;
    }

    [HttpGet(Name = "GetBooks")]
    public async Task<object?> GetBooks()
    {
        var requestBody = new
        {
            query = @"query { books { title author } }"
        };
        return await _httpClientService.PostAsync<object, object>("graphql", requestBody, _configuration["GeneralSettings:HttpServerBaseAddress"]);
    }
}