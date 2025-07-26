namespace communication_tech.Interfaces;

public interface IRedisQueueService
{
    Task EnqueueMessageAsync(string messageId, string message);
    Task<(string? MessageId, string? Message)> DequeueMessageAsync();
    Task<long> MessageCountAsync();
}