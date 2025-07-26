using communication_tech.Interfaces;
using StackExchange.Redis;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Communication.Tech.Consumer.Consumers;

public class KafkaConsumer : BackgroundService
{
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly IDatabase _redisDb;
    private readonly IPrometheusMetricService _prometheusMetricService;

    public KafkaConsumer(
        IConfiguration configuration, 
        ILogger<KafkaConsumer> logger,
        IConnectionMultiplexer redisConnection,
        IPrometheusMetricService prometheusMetricService)
    {
        _bootstrapServers = configuration["Kafka:BootstrapServers"];
        _topic = configuration["Kafka:Topic"];
        _groupId = configuration["Kafka:GroupId"];
        _logger = logger;
        _redisDb = redisConnection.GetDatabase();
        _prometheusMetricService = prometheusMetricService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_topic);

        _logger.LogInformation("Kafka Consumer started for topic: {Topic}", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    var split = result.Message.Value.Split(':', 2);

                    var messageId = split[0];

                    // Redis timestamp key
                    var tsKey = $"enqueue:{messageId}";

                    var enqueueTimeStr = await _redisDb.StringGetAsync(tsKey);
                    if (long.TryParse(enqueueTimeStr, out var enqueueMs))
                    {
                        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        var durationMs = nowMs - enqueueMs;
                        var durationSec = durationMs / 1000.0;

                        _prometheusMetricService.RecordMessageQueueTurnaround(messageId, "default", "kafka", durationSec);
                        await _redisDb.KeyDeleteAsync(tsKey);

                        _logger.LogInformation("✅  Consumed message: {MessageId}, turnaround = {Duration}ms", messageId, durationSec.ToString("F3"));
                    }
                    else
                    {
                        _logger.LogWarning("No enqueue timestamp found in Redis for messageId: {MessageId}", messageId);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {ErrorReason}", ex.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer is shutting down...");
        }

        await Task.CompletedTask;
    }
}
