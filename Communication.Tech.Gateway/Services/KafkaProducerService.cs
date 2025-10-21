using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using communication_tech.Interfaces;
using communication_tech.Models;
using Confluent.Kafka;
using StackExchange.Redis;

namespace communication_tech.Services;

public class KafkaProducerService
{
    private readonly KafkaSettings _settings;
    private readonly IDatabase _redisDb;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IPrometheusMetricService _prometheusMetricService;
    private readonly IProducer<Null, string> _producer;

    public KafkaProducerService(IConfiguration configuration, IConnectionMultiplexer redisConnection, ILogger<KafkaProducerService> logger, IPrometheusMetricService prometheusMetricService)
    {
        _settings = configuration.GetSection("Kafka").Get<KafkaSettings>()!;
        _redisDb = redisConnection.GetDatabase();
        _logger = logger;
        _prometheusMetricService = prometheusMetricService;
        
        WaitForKafkaAsync(_settings.BootstrapServers).GetAwaiter().GetResult();
        
        var config = GetProducerConfig();
        _producer = new ProducerBuilder<Null, string>(config)
            .SetErrorHandler((_, error) => _logger.LogError($"Kafka Error: {error.Code} - {error.Reason}"))
            .Build();
    }
    
    async Task WaitForKafkaAsync(string bootstrapServers)
    {
        var host = bootstrapServers.Split(':')[0];
        var port = int.Parse(bootstrapServers.Split(':')[1]);

        for (var i = 0; i < 12; i++)
        {
            try
            {
                // DNS resolving
                var addresses = await Dns.GetHostAddressesAsync(host);
                _logger.LogInformation($"✅ DNS resolved for {host}: {string.Join(", ", addresses.Select(a => a.ToString()))}");

                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(2000);

                if (await Task.WhenAny(connectTask, timeoutTask) == connectTask && tcp.Connected)
                {
                    _logger.LogInformation($"✅ Kafka is available at {host}:{port}!");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Kafka connection attempt {i + 1} failed: {ex.Message}");
            }

            _logger.LogInformation($"Kafka not ready, retrying in 5s... (attempt {i + 1}/12)");
            await Task.Delay(5000);
        }

        throw new Exception("Kafka is not reachable!");
    }

    public async Task ProduceAsync(string message)
    {
        var messageId = Guid.NewGuid().ToString();

        try
        {
            // ✅ Response Time
            var responseTimeWatch = Stopwatch.StartNew();
            
            // ✅ Turnaround Time
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _redisDb.StringSetAsync($"enqueue:{messageId}", now);
            
            // ✅ Latency 
            var latencyWatch = Stopwatch.StartNew();

            var deliveryResult = await _producer.ProduceAsync(_settings.Topic, 
                new Message<Null, string> { Value = $"{messageId}:{message}" });
            
            latencyWatch.Stop();
            
            responseTimeWatch.Stop();
            
            _prometheusMetricService.RecordKafkaLatency("producer-latency", latencyWatch.Elapsed.TotalSeconds);
            _prometheusMetricService.RecordKafkaResponseTime("producer-response_time", responseTimeWatch.Elapsed.TotalSeconds);
            
            _logger.LogInformation($"✅ Produced message: {messageId}, Partition: {deliveryResult.Partition}, Offset: {deliveryResult.Offset}");
            _logger.LogInformation($"Timestamp set: enqueue:{messageId} = {now}ms");
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError($"❌ Kafka produce error: {ex.Error.Code} - {ex.Error.Reason}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Unexpected error: {ex.Message}");
            throw;
        }
    }

    private ProducerConfig GetProducerConfig()
    {
        return new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            
            // Retry and timeout 
            MessageTimeoutMs = 30000,
            RequestTimeoutMs = 30000,
            SocketTimeoutMs = 60000,
            
            // Metadata refresh 
            MetadataMaxAgeMs = 180000,
            TopicMetadataRefreshIntervalMs = 10000,
            
            // DNS 
            BrokerAddressTtl = 1000,
            
            // Debug 
            Debug = "broker,topic,msg"
        };
    }
}