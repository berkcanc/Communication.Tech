using communication_tech.Interfaces;
using communication_tech.Models;
using communication_tech.Services;
using Communication.Tech.Protos;
using Microsoft.AspNetCore.Mvc;

namespace communication_tech.Controllers;

[ApiController]
[Route("[controller]")]
public class HTTPController : ControllerBase
{
    private readonly IPayloadGeneratorService _payloadGeneratorService;
    private readonly HttpClientService _httpClientService;
    private readonly IConfiguration _configuration;

    public HTTPController(IPayloadGeneratorService payloadGeneratorService, HttpClientService httpClientService, IConfiguration configuration)
    {
        _payloadGeneratorService = payloadGeneratorService;
        _httpClientService = httpClientService;
        _configuration = configuration;
    }
    
    [HttpGet(Name = "HelloMessage")]
    public async Task<ApiResponse?> HelloMessage([FromQuery] string message, [FromQuery] int sizeInKB)
    {
        var payload = _payloadGeneratorService.GenerateMessage(message, sizeInKB);
        var request = new ApiRequest(payload);
        return await _httpClientService.PostAsync<ApiRequest, ApiResponse>("HTTPServer", request, _configuration["GeneralSettings:HttpServerBaseAddress"]);
    }
}