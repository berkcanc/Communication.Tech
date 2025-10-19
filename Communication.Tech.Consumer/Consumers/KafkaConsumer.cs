using System.Diagnostics;
using System.Net;
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
    }
    
    private async Task WaitForKafkaAsync(string bootstrapServers)
    {
        const int retries = 12;
        const int delayInMs = 5000;
        
        var host = bootstrapServers.Split(':')[0];
        
        for (var i = 0; i < retries; i++)
        {
            try
            {
                // DNS resolving
                var addresses = await Dns.GetHostAddressesAsync(host);
                _logger.LogInformation("✅ DNS resolved for {Host}: {Addresses}", 
                    host, 
                    string.Join(", ", addresses.Select(a => a.ToString())));

                // Broker check with AdminClient  
                var config = new AdminClientConfig 
                { 
                    BootstrapServers = bootstrapServers,
                    SocketTimeoutMs = 10000
                };
                
                using var adminClient = new AdminClientBuilder(config)
                    .SetLogHandler((_, logMessage) =>
                    {
                        _logger.LogDebug("Kafka Admin Log: {Level} - {Message}", 
                            logMessage.Level, 
                            logMessage.Message);
                    })
                    .Build();
                    
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                
                if (metadata.Brokers.Count > 0)
                {
                    _logger.LogInformation("✅ Kafka broker is ready. Brokers found: {Count}", 
                        metadata.Brokers.Count);
                    
                    foreach (var broker in metadata.Brokers)
                    {
                        _logger.LogInformation("  - Broker {BrokerId}: {Host}:{Port}", 
                            broker.BrokerId, 
                            broker.Host, 
                            broker.Port);
                    }
                    
                    return;
                }
                
                _logger.LogWarning("Metadata retrieved but no brokers found");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Kafka not ready yet (attempt {Attempt}/{MaxAttempts}): {Message}", 
                    i + 1, 
                    retries, 
                    ex.Message);
            }

            _logger.LogInformation("Waiting for Kafka... retry {Retry}/{MaxRetries}", i + 1, retries);
            await Task.Delay(delayInMs);
        }

        throw new Exception("Kafka broker is not available after retries.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await WaitForKafkaAsync(_settings.BootstrapServers);
            _logger.LogInformation("✅ Kafka is ready. Starting consumer loop...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to connect to Kafka. Consumer will not start.");
            return;
        }

        var config = GetConsumerConfig();

        using var consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetLogHandler((_, logMessage) =>
            {
                if (logMessage.Level <= SyslogLevel.Warning)
                {
                    _logger.LogWarning("Kafka Consumer Log: {Level} - {Message}", 
                        logMessage.Level, 
                        logMessage.Message);
                }
                else
                {
                    _logger.LogDebug("Kafka Consumer Log: {Level} - {Message}", 
                        logMessage.Level, 
                        logMessage.Message);
                }
            })
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka Consumer Error: {Code} - {Reason} - IsFatal: {IsFatal}", 
                    error.Code, 
                    error.Reason, 
                    error.IsFatal);
            })
            .Build();

        try
        {
            consumer.Subscribe(_settings.Topic);
            _logger.LogInformation("✅ Kafka Consumer subscribed to topic: {Topic}", _settings.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to subscribe to topic: {Topic}", _settings.Topic);
            return;
        }

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
                    
                    var result = consumer.Consume(TimeSpan.FromSeconds(1)); // Timeout
                    
                    if (result == null)
                    {
                        // When timeout and then continue
                        continue;
                    }

                    latencyWatch.Stop();

                    _prometheusConsumerMetricService.RecordKafkaLatency("consumer-latency", latencyWatch.Elapsed.TotalSeconds);
                    
                    var split = result.Message.Value.Split(':', 2);
                    
                    if (split.Length < 1)
                    {
                        _logger.LogWarning("Invalid message format: {Message}", result.Message.Value);
                        continue;
                    }
                    
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
                            "✅ Consumed message: {MessageId} from Partition: {Partition}, Offset: {Offset}, " +
                            "Latency: {Latency}ms, " +
                            "Response Time: {ResponseTime}ms, " +
                            "Turnaround: {Turnaround}s",
                            messageId,
                            result.Partition.Value,
                            result.Offset.Value,
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
                    _logger.LogError(ex, "❌ Kafka consume error: {ErrorCode} - {ErrorReason}", 
                        ex.Error.Code, 
                        ex.Error.Reason);
                    
                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical("Fatal error detected. Attempting to reconnect...");
                        await Task.Delay(5000, stoppingToken);
                        throw; // BackgroundService restart
                    }
                }
                catch (KafkaException ex)
                {
                    _logger.LogError(ex, "❌ Kafka exception: {ErrorCode} - {ErrorReason}", 
                        ex.Error.Code, 
                        ex.Error.Reason);
                    
                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical("Fatal Kafka error. Service will restart.");
                        throw;
                    }
                    
                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Unexpected error while consuming message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer is shutting down...");
        }
        finally
        {
            try
            {
                consumer.Close();
                _logger.LogInformation("✅ Kafka consumer closed gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while closing consumer");
            }
        }
    }

    private ConsumerConfig GetConsumerConfig()
    {
        return new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            
            // Timeout 
            SocketTimeoutMs = 60000,
            SessionTimeoutMs = 45000,
            
            // Consumer 
            EnableAutoCommit = true,
            AutoCommitIntervalMs = 5000,
            EnableAutoOffsetStore = true,
            
            // DNS and metadata 
            MetadataMaxAgeMs = 180000,
            TopicMetadataRefreshIntervalMs = 10000,
            
        };
    }
}