using communication_tech.Interfaces;
using communication_tech.Models;
using communication_tech.Services;
using Microsoft.AspNetCore.Mvc;

namespace communication_tech.Controllers;

[ApiController]
[Route("[controller]")]
public class RedisController : ControllerBase
{
    private readonly IRedisQueueService _queueService;
    private readonly IPayloadGeneratorService _payloadGeneratorService;

    public RedisController(IRedisQueueService queueService, IPayloadGeneratorService payloadGeneratorService)
    {
        _queueService = queueService;
        _payloadGeneratorService = payloadGeneratorService;
    }
    
    [HttpPost("enqueue")]
    public async Task<IActionResult> Enqueue([FromBody] ProduceRequest request)
    {
        var id = Guid.NewGuid().ToString();
        var payload = _payloadGeneratorService.GenerateMessage(request.Message, request.SizeInKB);
        await _queueService.EnqueueMessageAsync(id, payload);
        return Ok(new { status = "queued", messageId = id, payload });
    }
    
    [HttpGet("count")]
    public async Task<IActionResult> GetQueueMessageCount()
    {
        var count = await _queueService.MessageCountAsync();
        return Ok(new { messageCount = count });
    }
}