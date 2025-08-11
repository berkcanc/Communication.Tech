using communication_tech.Models;
using Confluent.Kafka;
using StackExchange.Redis;

namespace communication_tech.Services;

public class KafkaProducerService
{
    private readonly KafkaSettings _settings;
    private readonly IDatabase _redisDb;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration configuration,  IConnectionMultiplexer redisConnection,  ILogger<KafkaProducerService> logger)
    {
        _settings = configuration.GetSection("Kafka").Get<KafkaSettings>()!;
        _redisDb = redisConnection.GetDatabase();
        _logger = logger;
    }

    public async Task ProduceAsync(string message)
    {
        var messageId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redisDb.StringSetAsync($"enqueue:{messageId}", now);

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            BrokerAddressFamily = BrokerAddressFamily.V6
        };

        using var producer = new ProducerBuilder<Null, string>(config).Build();

        try
        {
            await producer.ProduceAsync(_settings.Topic, new Message<Null, string> { Value = $"{messageId}:{message}"});
            Console.WriteLine($"✅ Produced message: {messageId}, Timestamp set: enqueue:{messageId} with = {now}ms");
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogInformation($"❌ Kafka produce error: {ex.Error.Reason}");
        }
    }
}