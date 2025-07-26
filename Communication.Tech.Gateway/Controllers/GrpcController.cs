using communication_tech.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Communication.Tech.Server;

namespace communication_tech.Controllers;

[ApiController]
[Route("[controller]")]
public class GrpcController : ControllerBase
{
    private readonly IPayloadGeneratorService _payloadGeneratorService;
    private readonly Greeter.GreeterClient _grpcClient;

    public GrpcController(IPayloadGeneratorService payloadGeneratorService, Greeter.GreeterClient grpcClient)
    {
        _payloadGeneratorService = payloadGeneratorService;
        _grpcClient = grpcClient;
    }

    [HttpGet("sayhello")]
    public async Task<IActionResult> SayHello([FromQuery] string message, [FromQuery] int sizeInKB)
    {
        var payload = _payloadGeneratorService.GenerateMessage(message, sizeInKB);
        var request = new HelloRequest { Name = payload};
        var reply = await _grpcClient.SayHelloAsync(request);
        return Ok(new { response = reply.Message });
        
    }
}