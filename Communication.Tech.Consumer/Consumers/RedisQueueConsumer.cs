using communication_tech.Interfaces;
using communication_tech.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Communication.Tech.Consumer.Consumers;

public class RedisQueueConsumer : BackgroundService
{
    private readonly ILogger<RedisQueueConsumer> _logger;
    private readonly IRedisQueueService _redisQueueService;

    public RedisQueueConsumer(ILogger<RedisQueueConsumer> logger, IRedisQueueService redisQueueService)
    {
        _logger = logger;
        _redisQueueService = redisQueueService;
        _logger.LogInformation("🟢 RedisQueueConsumer constructor çağrıldı.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("✅ Redis Queue Consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("🔄 Redis kuyruğu kontrol ediliyor: message_queue");

            try
            {
                var result = await _redisQueueService.DequeueMessageAsync();
                if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    _logger.LogInformation($"📥 Mesaj alındı: {result.MessageId} {result.Message}");
                }
                else
                {
                    // Kuyruk boşsa bir süre bekle
                    await Task.Delay(50, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Redis consumer error");
                await Task.Delay(1000, stoppingToken); // hata varsa biraz bekle
            }
        }

        _logger.LogWarning("🛑 Redis Queue Consumer durduruldu.");
    }

}