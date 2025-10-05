using communication_tech.Enums;
using communication_tech.Models;
using Microsoft.Extensions.Options;

namespace communication_tech.Services;

public class HttpMetricsCollector : BaseMetricsCollector<HttpMetric>
{
    public override TechnologyType TechnologyType => TechnologyType.Http;
    
    protected override string ThroughputQuery => 
        "sum(rate(http_turnaround_duration_seconds_count[5m]))";
    protected override string LatencyQuery => 
        "sum(rate(microsoft_aspnetcore_hosting_http_server_request_duration_sum{http_request_method=\"GET\", http_response_status_code=\"200\"}[5m]))/sum(rate(microsoft_aspnetcore_hosting_http_server_request_duration_count{http_request_method=\"GET\", http_response_status_code=\"200\"}[5m]))* 1000";
    protected override string ResponseTimeQuery =>
        "sum(rate(http_request_duration_seconds_sum[5m])) / sum(rate(http_request_duration_seconds_count[5m])) * 1000";
    protected override string TurnaroundTimeQuery => 
        "sum(rate(http_turnaround_duration_seconds_sum[5m])) / sum(rate(http_turnaround_duration_seconds_count[5m])) * 1000";

    public HttpMetricsCollector(HttpClient httpClient, IConfiguration config, ILogger<HttpMetricsCollector> logger, IOptions<PrometheusSettings> settings)
        : base(httpClient, config, logger, settings)
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

        _logger.LogDebug("Collected Http metrics - Throughput: {Throughput}, Latency: {Latency}ms", 
            metric.Throughput, metric.Latency);

        return metric;
    }
}