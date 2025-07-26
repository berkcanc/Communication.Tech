using System.Text;
using communication_tech.Interfaces;
using communication_tech.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace Communication.Tech.Consumer.Consumers;

public class RabbitMQConsumer : BackgroundService
{
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly IPrometheusMetricService _prometheusMetricService;
    private readonly IDatabase _redisDb;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQConsumer(
        IOptions<RabbitMqSettings> options,
        ILogger<RabbitMQConsumer> logger,
        IConnectionMultiplexer redis,
        IPrometheusMetricService prometheusMetricService)
    {
        _settings = options.Value;
        _logger = logger;
        _prometheusMetricService = prometheusMetricService;
        _redisDb = redis.GetDatabase();
        _logger.LogInformation("🟢 RabbitMQConsumer constructor initialized.");
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🟡 ExecuteAsync started (RabbitMQ)");

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        
        // Prefetch count ayarla (örneğin 10)
        _channel.BasicQos(0, 1, false);

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

                    _prometheusMetricService.RecordMessageQueueTurnaround(messageId, "default", "rabbitmq", durationSec);
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

        _channel.BasicConsume(queue: _settings.QueueName,
                              autoAck: false,
                              consumer: consumer);
        
        // consumer.Shutdown += (_, args) => {
        //     _logger.LogWarning("🔻 Consumer shutdown: {Reason}", args.ReplyText);
        //     return Task.CompletedTask;
        // };

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("🔻 RabbitMQ connection closed.");
        base.Dispose();
    }    
}
