using System.Text;
using communication_tech.Models;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace communication_tech.Services;

public class RabbitMQProducerService
{
    private readonly IModel _channel;
    private readonly RabbitMqSettings _settings;
    private readonly IDatabase _redisDb;
    private readonly ILogger<KafkaProducerService> _logger;

    public RabbitMQProducerService(IConfiguration config,   IConnectionMultiplexer redisConnection,  ILogger<KafkaProducerService> logger)
    {
        _redisDb = redisConnection.GetDatabase();
        _logger = logger;
        
        _settings = config.GetSection("RabbitMQ").Get<RabbitMqSettings>()!;

        var factory = new ConnectionFactory()
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password,
            SocketFactory = (addressFamily) => new NoDelayTcpClient(addressFamily)
        };
        
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();

        _channel.QueueDeclare(queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }
    
    public async Task SendMessageAsync(string message)
    {
        try
        {
            var messageId = Guid.NewGuid().ToString();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _redisDb.StringSetAsync($"enqueue:{messageId}", now, TimeSpan.FromMinutes(5));
        
            var body = Encoding.UTF8.GetBytes($"{messageId}:{message}");

            _channel.BasicPublish(
                exchange: "",
                routingKey: _settings.QueueName,
                basicProperties: null,
                body: body
            );
            
            Console.WriteLine($"✅ Published RabbitMQ message: {messageId}, Timestamp set: enqueue:{messageId} with = {now}ms");
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"❌ RabbitMQ produce error: {ex.StackTrace}");
        }
    }
}