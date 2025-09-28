using communication_tech.Enums;
using communication_tech.Models;
using Microsoft.Extensions.Options;

namespace communication_tech.Services;

public class RedisMetricsCollector : BaseMetricsCollector<RedisMetric>
{
    public override TechnologyType TechnologyType => TechnologyType.Redis;
    
    protected override string ThroughputQuery => "rate(redis_commands_processed_total[5m])";
    protected override string LatencyQuery => "redis_command_call_duration_seconds_sum / redis_command_call_duration_seconds_count * 1000";
    protected override string ResponseTimeQuery => "avg_over_time(redis_slowlog_length[5m])";
    protected override string TurnaroundTimeQuery => "histogram_quantile(0.95, rate(redis_command_call_duration_seconds_bucket[5m])) * 1000";

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