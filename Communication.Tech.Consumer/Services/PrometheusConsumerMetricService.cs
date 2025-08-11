using Communication.Tech.Consumer.Interfaces;
using Prometheus;

namespace Communication.Tech.Consumer.Services;

public class PrometheusConsumerMetricService : IPrometheusConsumerMetricService
{
    private readonly Histogram _turnaroundMessageQueueHistogram = Metrics.CreateHistogram("queue_turnaround_duration_seconds", "Turnaround duration", new HistogramConfiguration
    {
        Buckets = Histogram.LinearBuckets(0.01, 0.01, 100), // 10ms'den başlayarak 100 bucket = 1 saniyeye kadar
        LabelNames = new[] { "message_type", "source" }
    });
    
    public void RecordMessageQueueTurnaround(string messageId, string messageType, string source, double durationSeconds)
    {
        _turnaroundMessageQueueHistogram.WithLabels(messageType, source).Observe(durationSeconds);
        Console.WriteLine($"📊 Turnaround MQ metric: {source} - {messageId} = {durationSeconds:F3}s");
    }
}