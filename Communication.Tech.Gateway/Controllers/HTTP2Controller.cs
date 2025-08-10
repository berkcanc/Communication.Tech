using communication_tech.Interfaces;
using communication_tech.Models;
using communication_tech.Services;
using Communication.Tech.Protos;
using Microsoft.AspNetCore.Mvc;
namespace communication_tech.Controllers;

[ApiController]
[Route("[controller]")]
public class HTTP2Controller : ControllerBase
{
    private readonly IPayloadGeneratorService _payloadGeneratorService;
    private readonly HttpClientService _httpClientService;
    private readonly IConfiguration _configuration;
    
    public HTTP2Controller(IPayloadGeneratorService payloadGeneratorService, HttpClientService httpClientService, IConfiguration configuration)
    {
        _payloadGeneratorService = payloadGeneratorService;
        _httpClientService = httpClientService;
        _configuration = configuration;
    }
    
    [HttpGet(Name = "GetHelloMessage")]
    public async Task<ApiResponse?> GetHelloMessage([FromQuery] string message, [FromQuery] int sizeInKB)
    {
        var payload = _payloadGeneratorService.GenerateMessage(message, sizeInKB);
        var request = new ApiRequest(payload);
        return await _httpClientService.PostAsync<ApiRequest, ApiResponse>("HTTP2Server", request, _configuration["GeneralSettings:HttpServerBaseAddress"]);
    }
}