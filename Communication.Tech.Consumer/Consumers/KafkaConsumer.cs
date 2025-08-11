using communication_tech.Models;
using Communication.Tech.Consumer.Interfaces;
using StackExchange.Redis;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Communication.Tech.Consumer.Consumers;

public class KafkaConsumer : BackgroundService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly IDatabase _redisDb;
    private readonly IPrometheusConsumerMetricService _prometheusConsumerMetricService;

    public KafkaConsumer(
        IConfiguration configuration,
        ILogger<KafkaConsumer> logger,
        IConnectionMultiplexer redisConnection,
        IPrometheusConsumerMetricService prometheusConsumerMetricService)
    {
        _settings = configuration.GetSection("Kafka").Get<KafkaSettings>()!;
        _logger = logger;
        _redisDb = redisConnection.GetDatabase();
        _prometheusConsumerMetricService = prometheusConsumerMetricService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            BrokerAddressFamily = BrokerAddressFamily.V6
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_settings.Topic);

        _logger.LogInformation("Kafka Consumer started for topic: {Topic}", _settings.Topic);

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

                        _prometheusConsumerMetricService.RecordMessageQueueTurnaround(messageId, "default", "kafka", durationSec);
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
                catch (KafkaException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {ErrorReason}", ex.Error.Reason);
                    await Task.Delay(5000, stoppingToken);
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
