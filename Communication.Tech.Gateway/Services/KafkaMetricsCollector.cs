using communication_tech.Enums;
using communication_tech.Models;
using Microsoft.Extensions.Options;

namespace communication_tech.Services;

public class KafkaMetricsCollector : BaseMetricsCollector<KafkaMetric>
{
    public override TechnologyType TechnologyType => TechnologyType.Kafka;
    
    protected override string ThroughputQuery =>
        "sum(increase(kafka_topic_partition_current_offset{topic=\"test-topic\"}[5m]))";
    protected override string LatencyQuery =>
        "(avg(rate(kafka_producer_latency_seconds_sum[5m]) / rate(kafka_producer_latency_seconds_count[5m])) + avg(rate(kafka_consumer_latency_seconds_sum[5m]) / rate(kafka_consumer_latency_seconds_count[5m]))) * 1000 / 2";
    protected override string ResponseTimeQuery =>
        "(avg(rate(kafka_producer_response_time_seconds_sum[5m]) / rate(kafka_producer_response_time_seconds_count[5m])) + avg(rate(kafka_consumer_response_time_seconds_sum[5m]) / rate(kafka_consumer_response_time_seconds_count[5m]))) * 1000 / 2";
    protected override string TurnaroundTimeQuery =>
        "(sum(rate(queue_turnaround_duration_seconds_sum{source=\"kafka\"}[5m])) / sum(rate(queue_turnaround_duration_seconds_count{source=\"kafka\"}[5m]))) * 1000";
    
    public KafkaMetricsCollector(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<KafkaMetricsCollector> logger,
        IOptions<PrometheusSettings> settings)
        : base(httpClient, config, logger, settings)
    {
    }

    public override async Task<KafkaMetric> CollectAsync()
    {
        var metric = new KafkaMetric
        {
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };

        var tasks = new[]
        {
            ExecuteSimpleQueryAsync(ThroughputQuery),
            ExecuteSimpleQueryAsync(LatencyQuery),
            ExecuteSimpleQueryAsync(ResponseTimeQuery),
            ExecuteSimpleQueryAsync(TurnaroundTimeQuery),
            ExecuteSimpleQueryAsync(CpuUsageQuery),
            ExecuteSimpleQueryAsync(MemoryUsageQuery)
        };

        var results = await Task.WhenAll(tasks);

        metric.Throughput = results[0];
        metric.Latency = results[1];
        metric.ResponseTime = results[2];
        metric.TurnaroundTime = results[3];
        metric.CpuUsage = results[4];
        metric.MemoryUsage = results[5];

        _logger.LogDebug(
            "Collected Kafka metrics - Throughput: {Throughput}, Latency: {Latency}ms",
            metric.Throughput, metric.Latency
        );

        return metric;
    }
}