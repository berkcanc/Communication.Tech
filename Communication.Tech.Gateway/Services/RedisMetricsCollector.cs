using communication_tech.Enums;
using communication_tech.Models;

namespace communication_tech.Services;

public class RedisMetricsCollector : BaseMetricsCollector<RedisMetric>
{
    public override TechnologyType TechnologyType => TechnologyType.Redis;
    
    protected override string ThroughputQuery => "rate(redis_commands_processed_total[5m])";
    protected override string LatencyQuery => "redis_command_call_duration_seconds_sum / redis_command_call_duration_seconds_count * 1000";
    protected override string ResponseTimeQuery => "avg_over_time(redis_slowlog_length[5m])";
    protected override string TurnaroundTimeQuery => "histogram_quantile(0.95, rate(redis_command_call_duration_seconds_bucket[5m])) * 1000";

    public RedisMetricsCollector(HttpClient httpClient, IConfiguration config, ILogger<RedisMetricsCollector> logger)
        : base(httpClient, config, logger)
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
            ExecuteSimpleQueryAsync(TurnaroundTimeQuery)
        };

        var results = await Task.WhenAll(tasks);

        metric.Throughput = results[0];
        metric.Latency = results[1];
        metric.ResponseTime = results[2];
        metric.TurnaroundTime = results[3];

        _logger.LogDebug("Collected Redis metrics - Throughput: {Throughput}, Latency: {Latency}ms", 
            metric.Throughput, metric.Latency);

        return metric;
    }
}