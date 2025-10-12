using System.Diagnostics;
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
    private readonly IPrometheusMetricService _prometheusMetricService;

    public RedisController(IPayloadGeneratorService payloadGeneratorService, IConnectionMultiplexer redisConnection, IPrometheusMetricService prometheusMetricService)
    {
        _redisDb = redisConnection.GetDatabase();
        _payloadGeneratorService = payloadGeneratorService;
        _prometheusMetricService = prometheusMetricService;
    }
    
    [HttpPost("enqueue")]
    public async Task<IActionResult> Enqueue([FromBody] ProduceRequest request)
    {
        var id = Guid.NewGuid().ToString();
        var payload = _payloadGeneratorService.GenerateMessage(request.Message, request.SizeInKB);
    
        // ⏱️ LPUSH LATENCY
        var lpushStopwatch = Stopwatch.StartNew();
        await _redisDb.ListLeftPushAsync(QueueKey, $"{id}:{payload}");
        lpushStopwatch.Stop();
    
        // ⏱️ STRING SET LATENCY
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var setStopwatch = Stopwatch.StartNew();
        await _redisDb.StringSetAsync($"enqueue:{id}", nowMs);
        setStopwatch.Stop();

        _prometheusMetricService.RecordRedisLatency("lpush", lpushStopwatch.Elapsed.TotalSeconds);
        _prometheusMetricService.RecordRedisLatency("set", setStopwatch.Elapsed.TotalSeconds);
    
        // 📊 TOTAL RESPONSE TIME (LPUSH + SET)
        var totalLatency = lpushStopwatch.Elapsed.TotalMilliseconds + setStopwatch.Elapsed.TotalMilliseconds;
        _prometheusMetricService.RecordRedisResponseTime("enqueue", totalLatency / 1000.0);

        Console.WriteLine($"✅ Enqueued: {id}, LPUSH: {lpushStopwatch.Elapsed.TotalMilliseconds:F3}ms, SET: {setStopwatch.Elapsed.TotalMilliseconds:F3}ms");
    
        return Ok(new { 
            status = "queued", 
            messageId = id, 
            payload,
            metrics = new {
                lpushLatencyMs = lpushStopwatch.Elapsed.TotalMilliseconds,
                setLatencyMs = setStopwatch.Elapsed.TotalMilliseconds,
                totalResponseTimeMs = totalLatency
            }
        });
    }
    
    [HttpGet("count")]
    public async Task<IActionResult> GetQueueMessageCount()
    {
        var count = await _redisDb.ListLengthAsync(QueueKey);
        return Ok(new { messageCount = count });
    }
}