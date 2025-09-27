using communication_tech.Enums;
using communication_tech.Models;

namespace communication_tech.Services;

public class HttpMetricsCollector : BaseMetricsCollector<HttpMetric>
{
    public override TechnologyType TechnologyType => TechnologyType.Http;
    
    protected override string ThroughputQuery => "rate(http_requests_total[5m])";
    protected override string LatencyQuery => "histogram_quantile(0.50, rate(http_request_duration_seconds_bucket[5m])) * 1000";
    protected override string ResponseTimeQuery => "rate(http_request_duration_seconds_sum[5m]) / rate(http_request_duration_seconds_count[5m]) * 1000";
    protected override string TurnaroundTimeQuery => "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) * 1000";

    public HttpMetricsCollector(HttpClient httpClient, IConfiguration config, ILogger<HttpMetricsCollector> logger)
        : base(httpClient, config, logger)
    {
    }

    public override async Task<HttpMetric> CollectAsync()
    {
        var metric = new HttpMetric
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

        _logger.LogDebug("Collected Http metrics - Throughput: {Throughput}, Latency: {Latency}ms", 
            metric.Throughput, metric.Latency);

        return metric;
    }
}