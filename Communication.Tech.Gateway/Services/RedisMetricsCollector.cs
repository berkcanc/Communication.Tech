using communication_tech.Enums;
using communication_tech.Models;
using Microsoft.Extensions.Options;

namespace communication_tech.Services;

public class RedisMetricsCollector : BaseMetricsCollector<RedisMetric>
{
    public override TechnologyType TechnologyType => TechnologyType.Redis;
    
    protected override string ThroughputQuery => "sum(rate(redis_commands_total{cmd=~\"lpush|rpop\"}[1m]))";
    protected override string LatencyQuery => 
        "avg((rate(redis_producer_command_duration_seconds_sum{command=\"lpush\"}[5m]) / rate(redis_producer_command_duration_seconds_count{command=\"lpush\"}[5m])) or (rate(redis_consumer_command_duration_seconds_sum{command=\"rpop\"}[5m]) / rate(redis_consumer_command_duration_seconds_count{command=\"rpop\"}[5m]))) * 1000";
    protected override string ResponseTimeQuery => 
        "avg((rate(redis_producer_response_time_seconds_sum{operation=\"enqueue\"}[5m]) / rate(redis_producer_response_time_seconds_count{operation=\"enqueue\"}[5m])) or (rate(redis_consumer_response_time_seconds_sum{operation=\"dequeue\"}[5m]) / rate(redis_consumer_response_time_seconds_count{operation=\"dequeue\"}[5m]))) * 1000";
    protected override string TurnaroundTimeQuery => 
        "(sum(rate(queue_turnaround_duration_seconds_sum{source=\"redis\"}[5m])) / sum(rate(queue_turnaround_duration_seconds_count{source=\"redis\"}[5m]))) * 1000";

    public RedisMetricsCollector(HttpClient httpClient, IConfiguration config, ILogger<RedisMetricsCollector> logger, IOptions<PrometheusSettings> settings)
        : base(httpClient, config, logger, settings)
    {
    }

    public override async Task<RedisMetric> CollectAsync()
    {
        var metric = new RedisMetric
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

        _logger.LogDebug("Collected Redis metrics - Throughput: {Throughput}, Latency: {Latency}ms", 
            metric.Throughput, metric.Latency);

        return metric;
    }
}