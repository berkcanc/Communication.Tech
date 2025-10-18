using System.Diagnostics;
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

    public KafkaProducerService(IConfiguration configuration,  IConnectionMultiplexer redisConnection,  ILogger<KafkaProducerService> logger, IPrometheusMetricService prometheusMetricService)
    {
        _settings = configuration.GetSection("Kafka").Get<KafkaSettings>()!;
        _redisDb = redisConnection.GetDatabase();
        _logger = logger;
        _prometheusMetricService = prometheusMetricService;
        
        WaitForKafkaAsync(_settings.BootstrapServers).GetAwaiter().GetResult();
    }
    
    async Task WaitForKafkaAsync(string bootstrapServers)
    {
        var host = bootstrapServers.Split(':')[0];
        var port = int.Parse(bootstrapServers.Split(':')[1]);

        for (var i = 0; i < 12; i++)
        {
            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(2000);

                if (await Task.WhenAny(connectTask, timeoutTask) == connectTask && tcp.Connected)
                {
                    Console.WriteLine("✅ Kafka is available!");
                    return;
                }
            }
            catch { }

            Console.WriteLine("Kafka not ready, retrying in 5s...");
            await Task.Delay(5000);
        }

        throw new Exception("Kafka is not reachable!");
    }


    public async Task ProduceAsync(string message)
    {
        var messageId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redisDb.StringSetAsync($"enqueue:{messageId}", now);

        // ✅ Response Time
        var responseTimeWatch = Stopwatch.StartNew();
        
        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            BrokerAddressFamily = BrokerAddressFamily.V6
        };

        using var producer = new ProducerBuilder<Null, string>(config).Build();

        try
        {
            // ✅ Latency 
            var latencyWatch = Stopwatch.StartNew();

            await producer.ProduceAsync(_settings.Topic, new Message<Null, string> { Value = $"{messageId}:{message}"});
            
            latencyWatch.Stop();
            _prometheusMetricService.RecordKafkaLatency("producer-latency", latencyWatch.ElapsedMilliseconds);
            Console.WriteLine($"✅ Produced message: {messageId}, Timestamp set: enqueue:{messageId} with = {now}ms");
            
            responseTimeWatch.Stop();
            
            _prometheusMetricService.RecordKafkaResponseTime("producer-response_time", responseTimeWatch.Elapsed.TotalSeconds);
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogInformation($"❌ Kafka produce error: {ex.Error.Reason}");
        }
    }
}