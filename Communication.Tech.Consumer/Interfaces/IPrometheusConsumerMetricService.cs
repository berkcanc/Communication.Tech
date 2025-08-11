namespace Communication.Tech.Consumer.Interfaces;

public interface IPrometheusConsumerMetricService
{
    void RecordMessageQueueTurnaround(string messageId, string messageType, string source, double durationSeconds);
}