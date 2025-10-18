using System.Diagnostics;
using communication_tech.Models;
using Communication.Tech.Consumer.Interfaces;
using StackExchange.Redis;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        
        WaitForKafkaAsync(_settings.BootstrapServers).GetAwaiter().GetResult();
    }
    
    private async Task WaitForKafkaAsync(string bootstrapServers)
    {
        var config = new AdminClientConfig { BootstrapServers = bootstrapServers };
        const int retries = 12;
        const int delayInMs = 5000;
        
        for (var i = 0; i < retries; i++)
        {
            try
            {
                using var adminClient = new AdminClientBuilder(config).Build();
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                if (metadata.Brokers.Count > 0)
                {
                    _logger.LogInformation("Kafka broker is ready. Brokers found: {Count}", metadata.Brokers.Count);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Kafka not ready yet: {Message}", ex.Message);
            }

            _logger.LogInformation("Waiting for Kafka... retry {Retry}/{MaxRetries}", i + 1, retries);
            await Task.Delay(delayInMs);
        }

        throw new Exception("Kafka broker is not available after retries.");
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
                    // ✅ Response Time
                    var responseTimeWatch = Stopwatch.StartNew();
                    
                    // ✅ Latency (consume)
                    var latencyWatch = Stopwatch.StartNew();
                    
                    var result = consumer.Consume(stoppingToken);

                    latencyWatch.Stop();

                    _prometheusConsumerMetricService.RecordKafkaLatency("consumer-latency", latencyWatch.Elapsed.TotalSeconds);
                    
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
                        
                        responseTimeWatch.Stop();
                    
                        // ✅ Response Time (parsing + Redis operations)
                        _prometheusConsumerMetricService.RecordKafkaResponseTime("consumer-response_time", responseTimeWatch.Elapsed.TotalSeconds);
                        
                        _prometheusConsumerMetricService.RecordMessageQueueTurnaround(messageId, "default", "kafka", durationSec);
                        await _redisDb.KeyDeleteAsync(tsKey);

                        _logger.LogInformation(
                            "✅ Consumed message: {MessageId}, " +
                            "Latency: {Latency}ms, " +
                            "Response Time: {ResponseTime}ms, " +
                            "Turnaround: {Turnaround}s",
                            messageId,
                            latencyWatch.ElapsedMilliseconds,
                            responseTimeWatch.ElapsedMilliseconds,
                            durationSec.ToString("F3")
                        );
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
