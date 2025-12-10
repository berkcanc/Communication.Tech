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
    private readonly object _channelLock = new object();
    private bool _isDisposed = false;

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

    private async Task ExecuteConsumerAsync(CancellationToken stoppingToken)
    {
        try
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {   
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("📩 Received message: {Message}", message[..Math.Min(100, message.Length)]);

                    var split = message.Split(':', 2);
                    if (split.Length < 2)
                    {
                        _logger.LogWarning("⚠️ Malformed message: {Message}", message);
                        await SafeBasicNackAsync(ea.DeliveryTag, false, false);
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
                        _redisDb.KeyDelete(tsKey);

                        _logger.LogInformation("✅ MessageId: {MessageId}, turnaround = {DurationMs}ms", messageId, durationMs);
                    }
                    else
                    {
                        _logger.LogWarning("❌ Redis timestamp not found for MessageId: {MessageId}", messageId);
                    }

                    await SafeBasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❗ Error processing message");
                    try
                    {
                        await SafeBasicNackAsync(ea.DeliveryTag, false, true);
                    }
                    catch (Exception nackEx)
                    {
                        _logger.LogError(nackEx, "❗ Error nacking message after exception");
                    }
                }
            };

            lock (_channelLock)
            {
                if (_channel?.IsOpen == true)
                {
                    _channel.BasicConsume(queue: _settings.QueueName,
                                          autoAck: false,
                                          consumerTag: "consumer",
                                          consumer: consumer);
                    _logger.LogInformation("✅ Started consuming messages from queue: {Queue}", _settings.QueueName);
                }
            }

            // Wait until connection is lost or cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 Consumer cancelled");
            throw;
        }
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🟡 ExecuteAsync started (RabbitMQ)");

        while (!stoppingToken.IsCancellationRequested && !_isDisposed)
        {
            try
            {
                await ConnectWithRetryAsync(stoppingToken);

                if (_connection == null || _channel == null)
                {
                    _logger.LogError("❌ Failed to establish RabbitMQ connection after all retries");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }

                _logger.LogInformation("✅ Connected to RabbitMQ queue: {Queue}", _settings.QueueName);

                // Register connection shutdown handler
                _connection.ConnectionShutdown += (sender, args) =>
                {
                    _logger.LogWarning("⚠️ RabbitMQ connection lost. ReplyCode: {ReplyCode}, ReplyText: {ReplyText}, Cause: {Cause}", 
                        args.ReplyCode, args.ReplyText, args.Cause);
                };

                // Execute consumer
                await ExecuteConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 ExecuteAsync cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in ExecuteAsync");
                
                // Cleanup before retry
                CleanupConnection();

                // Wait before retry
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private void CleanupConnection()
    {
        try
        {
            lock (_channelLock)
            {
                if (_channel?.IsOpen == true)
                {
                    try
                    {
                        _channel.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error closing channel");
                    }
                }
                _channel?.Dispose();
                _channel = null;

                if (_connection?.IsOpen == true)
                {
                    try
                    {
                        _connection.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error closing connection");
                    }
                }
                _connection?.Dispose();
                _connection = null;
            }

            _logger.LogInformation("🔻 Connection and channel cleaned up");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during cleanup");
        }
    }

    private async Task SafeBasicAckAsync(ulong deliveryTag, bool multiple)
    {
        try
        {
            lock (_channelLock)
            {
                if (_channel?.IsOpen == true)
                {
                    _channel.BasicAck(deliveryTag, multiple);
                }
                else
                {
                    _logger.LogWarning("⚠️ Channel is not open, cannot acknowledge message with tag {DeliveryTag}", deliveryTag);
                }
            }
        }
        catch (AlreadyClosedException ex)
        {
            _logger.LogWarning(ex, "⚠️ Channel already closed when acknowledging delivery tag {DeliveryTag}", deliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error acknowledging message with delivery tag {DeliveryTag}", deliveryTag);
        }

        await Task.CompletedTask;
    }

    private async Task SafeBasicNackAsync(ulong deliveryTag, bool multiple, bool requeue)
    {
        try
        {
            lock (_channelLock)
            {
                if (_channel?.IsOpen == true)
                {
                    _channel.BasicNack(deliveryTag, multiple, requeue);
                }
                else
                {
                    _logger.LogWarning("⚠️ Channel is not open, cannot nack message with tag {DeliveryTag}", deliveryTag);
                }
            }
        }
        catch (AlreadyClosedException ex)
        {
            _logger.LogWarning(ex, "⚠️ Channel already closed when nacking delivery tag {DeliveryTag}", deliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error nacking message with delivery tag {DeliveryTag}", deliveryTag);
        }

        await Task.CompletedTask;
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

                lock (_channelLock)
                {
                    _connection = factory.CreateConnection($"consumer-{Environment.MachineName}");
                    _channel = _connection.CreateModel();
                    
                    // Queue declaration with error handling
                    _channel.QueueDeclare(
                        queue: _settings.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
                    
                    _channel.BasicQos(0, 1, false);
                }
                
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
        _isDisposed = true;
        CleanupConnection();
        _logger.LogInformation("🔻 RabbitMQConsumer disposed.");
        base.Dispose();
    }
}