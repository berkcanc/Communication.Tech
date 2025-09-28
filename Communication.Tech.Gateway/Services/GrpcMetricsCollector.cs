using communication_tech.Enums;
using communication_tech.Models;
using Microsoft.Extensions.Options;

namespace communication_tech.Services;

public class GrpcMetricsCollector : BaseMetricsCollector<GrpcMetric>
{
    public override TechnologyType TechnologyType => TechnologyType.gRPC;
    
    protected override string ThroughputQuery => "rate(grpc_server_handled_total[5m])";
    protected override string LatencyQuery => "histogram_quantile(0.50, rate(grpc_server_handling_seconds_bucket[5m])) * 1000";
    protected override string ResponseTimeQuery => "rate(grpc_server_handling_seconds_sum[5m]) / rate(grpc_server_handling_seconds_count[5m]) * 1000";
    protected override string TurnaroundTimeQuery => "histogram_quantile(0.95, rate(grpc_server_handling_seconds_bucket[5m])) * 1000";

    public GrpcMetricsCollector(HttpClient httpClient, IConfiguration config, ILogger<GrpcMetricsCollector> logger, IOptions<PrometheusSettings> settings)
        : base(httpClient, config, logger, settings)
    {
    }

    public override async Task<GrpcMetric> CollectAsync()
    {
        var metric = new GrpcMetric
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

        _logger.LogDebug("Collected gRPC metrics - Throughput: {Throughput}, Latency: {Latency}ms", 
            metric.Throughput, metric.Latency);

        return metric;
    }
}