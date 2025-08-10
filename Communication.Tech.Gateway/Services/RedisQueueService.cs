using StackExchange.Redis;
using communication_tech.Interfaces;

public class RedisQueueService : IRedisQueueService
{
    private readonly IDatabase _db;
    private readonly IPrometheusMetricService _prometheusMetricService;
    private const string QueueKey = "message_queue";
    private const string Source = "redis";

    public RedisQueueService(IPrometheusMetricService prometheusMetricService, IConfiguration configuration)
    {
        _prometheusMetricService = prometheusMetricService;
        var redis = ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"] ?? string.Empty);
        _db = redis.GetDatabase();
    }

    public async Task EnqueueMessageAsync(string messageId, string message)
    {
        await _db.ListLeftPushAsync(QueueKey, $"{messageId}:{message}");

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _db.StringSetAsync($"enqueue:{messageId}", nowMs);

        Console.WriteLine($"✅ Enqueued: {messageId}, Timestamp set: enqueue:{messageId} = {nowMs}ms");
    }

    public async Task<(string? MessageId, string? Message)> DequeueMessageAsync()
    {
        var result = await _db.ListRightPopAsync(QueueKey);
        if (!result.HasValue) return (null, null);

        var split = result.ToString().Split(':', 2);
        if (split.Length < 2) return (null, result);

        var messageId = split[0];
        var message = split[1];

        var enqueueKey = $"enqueue:{messageId}";
        var enqueueTimeStr = await _db.StringGetAsync(enqueueKey);

        if (long.TryParse(enqueueTimeStr, out var enqueueMs))
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var durationMs = nowMs - enqueueMs;
            var durationSec = durationMs / 1000.0;

            _prometheusMetricService.RecordMessageQueueTurnaround(messageId, "default", Source, durationSec);
            await _db.KeyDeleteAsync(enqueueKey);

            Console.WriteLine($"📥 Dequeued: {messageId}, Turnaround: {durationMs}ms ({durationSec:F3}s)");
        }
        else
        {
            Console.WriteLine($"⚠️ No timestamp found for {enqueueKey}");
        }

        return (messageId, message);
    }

    public async Task<long> MessageCountAsync()
    {
        return await _db.ListLengthAsync(QueueKey);
    }
}