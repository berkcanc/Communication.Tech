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

    public HTTPController(IPayloadGeneratorService payloadGeneratorService, HttpClientService httpClientService)
    {
        _payloadGeneratorService = payloadGeneratorService;
        _httpClientService = httpClientService;
    }
    
    [HttpGet(Name = "HelloMessage")]
    public async Task<ApiResponse> HelloMessage([FromQuery] string message, [FromQuery] int sizeInKB)
    {
        var payload = _payloadGeneratorService.GenerateMessage(message, sizeInKB);
        var request = new ApiRequest { Message = payload };
        return await _httpClientService.PostAsync<ApiRequest, ApiResponse>("HTTPServer", request, Constants.HTTPBaseAddress);
    }
}