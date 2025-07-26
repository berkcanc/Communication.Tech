using communication_tech.Interfaces;
using Confluent.Kafka;
using StackExchange.Redis;

namespace communication_tech.Services;

public class KafkaProducerService
{
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly IDatabase _redisDb;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration configuration,  IConnectionMultiplexer redisConnection,  ILogger<KafkaProducerService> logger)
    {
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? string.Empty;
        _topic = configuration["Kafka:Topic"] ?? string.Empty;
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
            BootstrapServers = _bootstrapServers,
            BrokerAddressFamily = BrokerAddressFamily.V4
        };

        using var producer = new ProducerBuilder<Null, string>(config).Build();

        try
        {
            await producer.ProduceAsync(_topic, new Message<Null, string> { Value = $"{messageId}:{message}"});
            Console.WriteLine($"✅ Produced message: {messageId}, Timestamp set: enqueue:{messageId} with = {now}ms");
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogInformation($"❌ Kafka produce error: {ex.Error.Reason}");
        }
    }
}