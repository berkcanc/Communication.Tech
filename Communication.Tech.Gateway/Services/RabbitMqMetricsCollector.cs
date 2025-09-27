using communication_tech.Enums;
using communication_tech.Models;

namespace communication_tech.Services;

public class RabbitMqMetricsCollector : BaseMetricsCollector<RabbitMqMetric>
{
    private readonly string _queue;
    private readonly string _vhost;

    public override TechnologyType TechnologyType => TechnologyType.RabbitMQ;

    protected override string ThroughputQuery => $"rate(rabbitmq_queue_messages_ack_total{{queue=\"{_queue}\",vhost=\"{_vhost}\"}}[5m])";
    protected override string LatencyQuery => $"((rabbitmq_queue_messages_ready{{queue=\"{_queue}\"}} or vector(0) + rabbitmq_queue_messages_unacknowledged{{queue=\"{_queue}\"}} or vector(0)) / clamp_min(rate(rabbitmq_queue_messages_delivered_total{{queue=\"{_queue}\"}}[5m]), 1)) * 1000";
    protected override string ResponseTimeQuery => "(rate(queue_turnaround_duration_seconds_sum{source=\"rabbitmq\"}[5m]) / clamp_min(rate(queue_turnaround_duration_seconds_count{source=\"rabbitmq\"}[5m]), 0.01)) * 1000";
    protected override string TurnaroundTimeQuery => "1000*(sum(rate(queue_turnaround_duration_seconds_sum{source=\"rabbitmq\"}[5m])) / sum(rate(queue_turnaround_duration_seconds_count{source=\"rabbitmq\"}[5m])))";

    public RabbitMqMetricsCollector(HttpClient httpClient, IConfiguration config, ILogger<RabbitMqMetricsCollector> logger)
        : base(httpClient, config, logger)
    {
        _queue = config.GetValue<string>("RabbitMQ:Queue", "message_queue");
        _vhost = config.GetValue<string>("RabbitMQ:VHost", "/");
    }

    public override async Task<RabbitMqMetric> CollectAsync()
    {
        var metric = new RabbitMqMetric
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

        _logger.LogDebug("Collected RabbitMQ metrics - Throughput: {Throughput}, Latency: {Latency}ms", 
            metric.Throughput, metric.Latency);

        return metric;
    }
}