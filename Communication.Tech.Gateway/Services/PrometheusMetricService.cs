using communication_tech.Interfaces;
using Grpc.Core;
using Prometheus;

public class PrometheusMetricService : IPrometheusMetricService
{
    private readonly Histogram _turnaroundMessageQueueHistogram = Metrics.CreateHistogram("queue_turnaround_duration_seconds", "Turnaround duration", new HistogramConfiguration
    {
        Buckets = Histogram.LinearBuckets(0.01, 0.01, 100), // 10ms'den başlayarak 100 bucket = 1 saniyeye kadar
        LabelNames = new[] { "message_type", "source" }
    });
    
    private readonly Histogram _httpHistogram = Metrics.CreateHistogram("http_turnaround_duration_seconds", "HTTP turnaround", new HistogramConfiguration
    {
        Buckets = Histogram.ExponentialBuckets(0.01, 2, 10), // 10ms to ~10s
        LabelNames = new[] { "method", "path", "status_code" }
    });

    private readonly Histogram _grpcHistogram = Metrics.CreateHistogram("grpc_turnaround_duration_seconds", "gRPC turnaround", new HistogramConfiguration
    {
        Buckets = Histogram.ExponentialBuckets(0.01, 2, 10),
        LabelNames = new[] { "service", "method", "status_code" }
    });

    public void RecordMessageQueueTurnaround(string messageId, string messageType, string source, double durationSeconds)
    {
        _turnaroundMessageQueueHistogram.WithLabels(messageType, source).Observe(durationSeconds);
        Console.WriteLine($"📊 Turnaround metric: {messageId} = {durationSeconds:F3}s");
    }
    
    public void RecordHttpTurnaround(string method, string path, int statusCode, double durationSeconds)
    {
        _httpHistogram.WithLabels(method, path.ToLower(), statusCode.ToString()).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Turnaround metric: {method}, {path}, {statusCode} = {durationSeconds:F3}s");
    }

    public void RecordGrpcTurnaround(string service, string method, StatusCode statusCode, double durationSeconds)
    {
        _grpcHistogram.WithLabels(service, method, statusCode.ToString()).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Turnaround metric: {method}, {method}, {statusCode} = {durationSeconds:F3}s");
    }


}