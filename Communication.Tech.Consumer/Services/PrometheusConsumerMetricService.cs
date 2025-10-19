using Communication.Tech.Consumer.Interfaces;
using Prometheus;

namespace Communication.Tech.Consumer.Services;

public class PrometheusConsumerMetricService : IPrometheusConsumerMetricService
{
    private readonly Histogram _turnaroundMessageQueueHistogram = Metrics.CreateHistogram("queue_turnaround_duration_seconds", "Turnaround duration", new HistogramConfiguration
    {
        Buckets = Histogram.LinearBuckets(0.01, 0.01, 100), // 10ms - 1s - 100 bucket
        LabelNames = new[] { "message_type", "source" }
    });
    
    private static readonly Histogram _redisLatency = Metrics.CreateHistogram(
        "redis_consumer_command_duration_seconds",
        "Redis command latency in seconds (consumer side)",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.0001, 2, 15),
            LabelNames = new[] { "command" }
        });

    private static readonly Histogram _redisResponseTime = Metrics.CreateHistogram(
        "redis_consumer_response_time_seconds",
        "Redis operation response time in seconds (consumer side)",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.0001, 2, 15),
            LabelNames = new[] { "operation" }
        });
    
    private static readonly Histogram _kafkaLatencyHistogram = Metrics.CreateHistogram(
        "kafka_consumer_latency_seconds",
        "Time between when a Kafka message was produced and when it was consumed",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 15),
            LabelNames = new[] { "command" }
        });

    private static readonly Histogram _kafkaResponseTimeHistogram = Metrics.CreateHistogram(
        "kafka_consumer_response_time_seconds",
        "Time taken by consumer to process a consumed Kafka message",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 15),
            LabelNames = new[] { "operation" }
        });
    
    public void RecordMessageQueueTurnaround(string messageId, string messageType, string source, double durationSeconds)
    {
        _turnaroundMessageQueueHistogram.WithLabels(messageType, source).Observe(durationSeconds);
        Console.WriteLine($"📊 Turnaround MQ metric: {source} - {messageId} = {durationSeconds:F3}s");
    }
    
    public void RecordRedisLatency(string command, double durationSeconds)
    {
        _redisLatency.WithLabels(command).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Redis Latency metric: {command}, {durationSeconds:F3}s");
    }

    public void RecordRedisResponseTime(string operation, double durationSeconds)
    {
        _redisResponseTime.WithLabels(operation).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Redis Response Time metric: {operation}, {durationSeconds:F3}s");
    }
    
    public void RecordKafkaLatency(string command, double durationSeconds)
    {
        _kafkaLatencyHistogram.WithLabels(command).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Kafka Latency metric: {command}, {durationSeconds:F3}s");
    }

    public void RecordKafkaResponseTime(string operation, double durationSeconds)
    {
        _kafkaResponseTimeHistogram.WithLabels(operation).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Kafka Response Time metric: {operation}, {durationSeconds:F3}s");
    }
}