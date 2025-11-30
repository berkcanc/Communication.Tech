using System.Text;
using communication_tech;
using communication_tech.Models;
using Communication.Tech.Consumer.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using StackExchange.Redis;

namespace Communication.Tech.Consumer.Consumers;

public class RabbitMQConsumer : BackgroundService
{
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly IPrometheusConsumerMetricService _prometheusConsumerMetricService;
    private readonly IDatabase _redisDb;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQConsumer(
        IOptions<RabbitMqSettings> options,
        ILogger<RabbitMQConsumer> logger,
        IConnectionMultiplexer redis,
        IPrometheusConsumerMetricService prometheusConsumerMetricService)
    {
        _settings = options.Value;
        _logger = logger;
        _prometheusConsumerMetricService = prometheusConsumerMetricService;
        _redisDb = redis.GetDatabase();
        _logger.LogInformation("🟢 RabbitMQConsumer constructor initialized.");
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🟡 ExecuteAsync started (RabbitMQ)");

        await ConnectWithRetryAsync(stoppingToken);

        if (_connection == null || _channel == null)
        {
            _logger.LogError("❌ Failed to establish RabbitMQ connection after all retries");
            return;
        }

        _logger.LogInformation("✅ Connected to RabbitMQ queue: {Queue}", _settings.QueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {   
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("📩 Received message: {Message}", message);

                var split = message.Split(':', 2);
                if (split.Length < 2)
                {
                    _logger.LogWarning("⚠️ Malformed message: {Message}", message);
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                var messageId = split[0];
                var tsKey = $"enqueue:{messageId}";
                var enqueueTimeStr = await _redisDb.StringGetAsync(tsKey);

                if (long.TryParse(enqueueTimeStr, out var enqueueMs))
                {
                    var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var durationMs = nowMs - enqueueMs;
                    var durationSec = durationMs / 1000.0;

                    _prometheusConsumerMetricService.RecordMessageQueueTurnaround(messageId, "default", "rabbitmq", durationSec);
                    await _redisDb.KeyDeleteAsync(tsKey);

                    _logger.LogInformation("✅ MessageId: {MessageId}, turnaround = {DurationMs}ms", messageId, durationMs);
                }
                else
                {
                    _logger.LogWarning("❌ Redis timestamp not found for MessageId: {MessageId}", messageId);
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❗ Error processing message");
                _channel.BasicNack(ea.DeliveryTag, false, true); // mesajı kuyruğa geri koy
            }
        };

        // Connection lost handler
        _connection.ConnectionShutdown += async (sender, args) =>
        {
            _logger.LogWarning("⚠️ RabbitMQ connection lost. Reason: {Reason}", args.ReplyText);
            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                _logger.LogInformation("🔄 Attempting to reconnect to RabbitMQ...");
                await ConnectWithRetryAsync(stoppingToken);
            }
        };

        _channel.BasicConsume(queue: _settings.QueueName,
                              autoAck: false,
                              consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var maxRetries = 20;
        var retryCount = 0;
        var baseDelay = TimeSpan.FromSeconds(5);

        while (retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔄 Attempting to connect to RabbitMQ (Attempt {Attempt}/{MaxAttempts})", 
                    retryCount + 1, maxRetries);

                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    DispatchConsumersAsync = true,
                    SocketFactory = (addressFamily) => new NoDelayTcpClient(addressFamily),
                    
                    // Connection recovery settings
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    RequestedHeartbeat = TimeSpan.FromSeconds(60),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(60),
                    
                    // Socket configuration
                    SocketReadTimeout = TimeSpan.FromSeconds(30),
                    SocketWriteTimeout = TimeSpan.FromSeconds(30),
                    ContinuationTimeout = TimeSpan.FromSeconds(20),
                    HandshakeContinuationTimeout = TimeSpan.FromSeconds(20)
                };

                _connection = factory.CreateConnection($"consumer-{Environment.MachineName}");
                _channel = _connection.CreateModel();
                
                // Queue declaration with error handling
                _channel.QueueDeclare(
                    queue: _settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                
                _channel.BasicQos(0, 50, false);
                
                _logger.LogInformation("✅ Successfully connected to RabbitMQ on {Host}:{Port}", 
                    _settings.HostName, _settings.Port);
                return;
            }
            catch (BrokerUnreachableException ex)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Min(baseDelay.TotalSeconds * Math.Pow(2, retryCount - 1), 60));
                
                _logger.LogWarning(ex, "⚠️ Failed to connect to RabbitMQ. Retry {RetryCount}/{MaxRetries}. Waiting {Delay} seconds...", 
                    retryCount, maxRetries, delay.TotalSeconds);
                
                if (retryCount >= maxRetries)
                {
                    _logger.LogError("❌ Maximum retry attempts reached. Unable to connect to RabbitMQ.");
                    throw;
                }
                
                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Min(baseDelay.TotalSeconds * Math.Pow(2, retryCount - 1), 60));
                
                _logger.LogError(ex, "❌ Unexpected error connecting to RabbitMQ. Retry {RetryCount}/{MaxRetries}", 
                    retryCount, maxRetries);
                
                if (retryCount >= maxRetries)
                {
                    throw;
                }
                
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        try
        {
            if (_channel?.IsOpen == true)
            {
                _channel.Close();
            }
            if (_connection?.IsOpen == true)
            {
                _connection.Close();
            }
            _logger.LogInformation("🔻 RabbitMQ connection closed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing RabbitMQ connections");
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }    
}