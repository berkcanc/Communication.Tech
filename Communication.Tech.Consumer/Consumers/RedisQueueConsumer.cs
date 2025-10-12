using System.Diagnostics;
using Communication.Tech.Consumer.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Communication.Tech.Consumer.Consumers;

public class RedisQueueConsumer : BackgroundService
{
    private readonly ILogger<RedisQueueConsumer> _logger;
    private readonly IPrometheusConsumerMetricService _prometheusConsumerMetricService;
    private readonly IDatabase _redisDb;
    private const string QueueKey = "message_queue";
    private const string Source = "redis";
    public RedisQueueConsumer(ILogger<RedisQueueConsumer> logger, IConnectionMultiplexer redisConnection, IPrometheusConsumerMetricService prometheusConsumerMetricService)
    {
        _redisDb = redisConnection.GetDatabase();
        _logger = logger;
        _prometheusConsumerMetricService = prometheusConsumerMetricService;
        _logger.LogInformation("🟢 RedisQueueConsumer constructor initialized.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("✅ Redis Queue Consumer started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("🔄 Redis kuyruğu kontrol ediliyor: message_queue");

                try
                {
                    var result = await DequeueMessageAsync();
                    if (!string.IsNullOrWhiteSpace(result.Message))
                    {
                        _logger.LogInformation($"📥 Message received: {result.MessageId} {result.Message}");
                    }
                    else
                    {
                        // If the queue is empty, then wait
                        await Task.Delay(50, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Redis consumer error");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        _logger.LogWarning("🛑 Redis Queue Consumer stopped.");
    }
    
    
    private async Task<(string? MessageId, string? Message)> DequeueMessageAsync()
    {
        // ⏱️ RPOP LATENCY
        var rpopStopwatch = Stopwatch.StartNew();
        var result = await _redisDb.ListRightPopAsync(QueueKey);
        rpopStopwatch.Stop();
        
        if (!result.HasValue)
        {
            return (null, null);
        }

        var split = result.ToString().Split(':', 2);
        if (split.Length < 2) return (null, result);

        var messageId = split[0];
        var message = split[1];

        var enqueueKey = $"enqueue:{messageId}";
        
        // ⏱️ STRING GET LATENCY
        var getStopwatch = Stopwatch.StartNew();
        var enqueueTimeStr = await _redisDb.StringGetAsync(enqueueKey);
        getStopwatch.Stop();
        
        // ⏱️ KEY DELETE LATENCY
        var deleteStopwatch = Stopwatch.StartNew();
        await _redisDb.KeyDeleteAsync(enqueueKey);
        deleteStopwatch.Stop();

        _prometheusConsumerMetricService.RecordRedisLatency("rpop", rpopStopwatch.Elapsed.TotalSeconds);
        _prometheusConsumerMetricService.RecordRedisLatency("get", getStopwatch.Elapsed.TotalSeconds);
        _prometheusConsumerMetricService.RecordRedisLatency("delete", deleteStopwatch.Elapsed.TotalSeconds);
        
        // 📊 TOTAL RESPONSE TIME (RPOP + GET + DELETE)
        var totalResponseTime = rpopStopwatch.Elapsed.TotalMilliseconds + 
                               getStopwatch.Elapsed.TotalMilliseconds + 
                               deleteStopwatch.Elapsed.TotalMilliseconds;
        _prometheusConsumerMetricService.RecordRedisResponseTime("dequeue", totalResponseTime / 1000.0);

        if (long.TryParse(enqueueTimeStr, out var enqueueMs))
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var durationMs = nowMs - enqueueMs;
            var durationSec = durationMs / 1000.0;

            _prometheusConsumerMetricService.RecordMessageQueueTurnaround(messageId, "default", Source, durationSec);

            Console.WriteLine($"📥 Dequeued: {messageId}, Turnaround: {durationMs}ms, " +
                             $"RPOP: {rpopStopwatch.Elapsed.TotalMilliseconds:F3}ms, " +
                             $"GET: {getStopwatch.Elapsed.TotalMilliseconds:F3}ms, " +
                             $"DEL: {deleteStopwatch.Elapsed.TotalMilliseconds:F3}ms");
        }
        else
        {
            Console.WriteLine($"⚠️ No timestamp found for {enqueueKey}");
        }

        return (messageId, message);
    }

}