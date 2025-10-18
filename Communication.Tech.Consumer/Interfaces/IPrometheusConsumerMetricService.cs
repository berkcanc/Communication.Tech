namespace Communication.Tech.Consumer.Interfaces;

public interface IPrometheusConsumerMetricService
{
    void RecordMessageQueueTurnaround(string messageId, string messageType, string source, double durationSeconds);
    void RecordRedisLatency(string command, double durationSeconds);
    void RecordRedisResponseTime(string operation, double durationSeconds);
    void RecordKafkaLatency(string command, double durationSeconds);
    void RecordKafkaResponseTime(string operation, double durationSeconds);
}