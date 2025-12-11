using System.Text;
using communication_tech;
using communication_tech.Models;
using RabbitMQ.Client;
using StackExchange.Redis;

public class RabbitMQProducerService : IDisposable, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMqSettings _settings;
    private readonly IDatabase _redisDb;
    private readonly ILogger<RabbitMQProducerService> _logger;
    private readonly object _channelLock = new();
    private bool _disposed = false;

    public RabbitMQProducerService(IConfiguration config, IConnectionMultiplexer redisConnection, ILogger<RabbitMQProducerService> logger)
    {
        _redisDb = redisConnection.GetDatabase();
        _logger = logger;
        
        _settings = config.GetSection("RabbitMQ").Get<RabbitMqSettings>()!;

        var factory = new ConnectionFactory()
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password,
            SocketFactory = (addressFamily) => new NoDelayTcpClient(addressFamily),
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            AutomaticRecoveryEnabled = true,
            MaxMessageSize = 134217728 // 128MB
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.BasicQos(0, 10, false); // Prefetch 10 messages at a time

        _channel.QueueDeclare(queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        
        _logger.LogInformation("✅ RabbitMQ Producer initialized for queue: {Queue}", _settings.QueueName);
    }
    
    public async Task SendMessageAsync(string message)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMQProducerService));

        try
        {
            var messageId = Guid.NewGuid().ToString();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            await _redisDb.StringSetAsync($"enqueue:{messageId}", now, TimeSpan.FromMinutes(5));
        
            var body = Encoding.UTF8.GetBytes($"{messageId}:{message}");
            
            var properties = _channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // Persistent
            properties.Timestamp = new AmqpTimestamp(now);

            lock (_channelLock)
            {
                if (_channel?.IsOpen == true)
                {
                    _channel.BasicPublish(
                        exchange: "",
                        routingKey: _settings.QueueName,
                        basicProperties: properties,
                        body: body
                    );
                    
                    _logger.LogInformation("✅ Published RabbitMQ message: {MessageId}", messageId);
                }
                else
                {
                    _logger.LogError("❌ Channel is closed, cannot publish message");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ RabbitMQ produce error");
            throw; // Rethrow so caller knows it failed
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _disposed = true;
            _logger.LogInformation("✅ RabbitMQ Producer disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error disposing RabbitMQ Producer");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }
}
