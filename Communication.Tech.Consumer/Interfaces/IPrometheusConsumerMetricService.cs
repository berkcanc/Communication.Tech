namespace Communication.Tech.Consumer.Interfaces;

public interface IPrometheusConsumerMetricService
{
    void RecordRedisLatency(string command, double durationSeconds);
    void RecordRedisResponseTime(string operation, double durationSeconds);
    void RecordMessageQueueTurnaround(string messageId, string messageType, string source, double durationSeconds);
}