using communication_tech.Interfaces;
using communication_tech.Models;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace communication_tech.Controllers;

[ApiController]
[Route("[controller]")]
public class RedisController : ControllerBase
{
    private readonly IDatabase _redisDb;
    private const string QueueKey = "message_queue";
    private readonly IPayloadGeneratorService _payloadGeneratorService;

    public RedisController(IPayloadGeneratorService payloadGeneratorService, IConnectionMultiplexer redisConnection)
    {
        _redisDb = redisConnection.GetDatabase();
        _payloadGeneratorService = payloadGeneratorService;
    }
    
    [HttpPost("enqueue")]
    public async Task<IActionResult> Enqueue([FromBody] ProduceRequest request)
    {
        var id = Guid.NewGuid().ToString();
        var payload = _payloadGeneratorService.GenerateMessage(request.Message, request.SizeInKB);
        await _redisDb.ListLeftPushAsync(QueueKey, $"{id}:{payload}");

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redisDb.StringSetAsync($"enqueue:{id}", nowMs);

        Console.WriteLine($"✅ Enqueued: {id}, Timestamp set: enqueue:{id} = {nowMs}ms");
        return Ok(new { status = "queued", messageId = id, payload });
    }
    
    [HttpGet("count")]
    public async Task<IActionResult> GetQueueMessageCount()
    {
        var count = await _redisDb.ListLengthAsync(QueueKey);
        return Ok(new { messageCount = count });
    }
}