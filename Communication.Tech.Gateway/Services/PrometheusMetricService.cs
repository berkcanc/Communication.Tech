using System.Globalization;
using System.Text.Json;
using communication_tech.Interfaces;
using communication_tech.Models;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Prometheus;

namespace communication_tech.Services;

public class PrometheusMetricService : IPrometheusMetricService
{
    private readonly Histogram _turnaroundMessageQueueHistogram = Metrics.CreateHistogram("queue_turnaround_duration_seconds", "Turnaround duration", new HistogramConfiguration
    {
        Buckets = Histogram.LinearBuckets(0.01, 0.01, 100), // 10ms'den başlayarak 100 bucket = 1 saniyeye kadar
        LabelNames = ["message_type", "source"]
    });
    
    private readonly Histogram _httpHistogram = Metrics.CreateHistogram("http_turnaround_duration_seconds", "HTTP turnaround", new HistogramConfiguration
    {
        Buckets = Histogram.ExponentialBuckets(0.01, 2, 10), // 10ms to ~10s
        LabelNames = ["method", "path", "status_code"]
    });

    private readonly Histogram _grpcHistogram = Metrics.CreateHistogram("grpc_turnaround_duration_seconds", "gRPC turnaround", new HistogramConfiguration
    {
        Buckets = Histogram.ExponentialBuckets(0.01, 2, 10),
        LabelNames = ["service", "method", "status_code"]
    });
    
    private readonly HttpClientService _httpClientService;
    private readonly PrometheusSettings _prometheusSettings;

    public PrometheusMetricService(HttpClientService httpClientService, IOptions<PrometheusSettings> settings)
    {
        _httpClientService = httpClientService;
        _prometheusSettings = settings.Value;
    }

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

    public async Task<IEnumerable<MetricDataPoint>> GetMetricRangeDataAsync(string query, DateTime startTime,
        DateTime endTime, string step)
    {
        const string route = "api/v1/query_range";
        
        var startUnix = new DateTimeOffset(startTime).ToUnixTimeSeconds();
        var endUnix = new DateTimeOffset(endTime).ToUnixTimeSeconds();
        
        var queryParams = new Dictionary<string, string>
        {
            { "query", query },
            { "start", startUnix.ToString() },
            { "end", endUnix.ToString() },
            { "step", step }
        };

        var response = await _httpClientService.GetAsyncWithQueryString<PrometheusQueryResponse>(
            _prometheusSettings.Url
            , route
            , queryParams);
        
        if (response?.Data.Result == null || response.Data.Result.Count == 0)
            return [];

        return response.Data.Result
            .SelectMany(r => r.Values)
            .Select(val =>
            {
                var metricValueStr = ((JsonElement)val[1]).GetString();
                if (metricValueStr == "NaN")
                    metricValueStr = "0";
                    
                var parsedValue = decimal.Parse(metricValueStr, CultureInfo.InvariantCulture);
                var roundedValue = Math.Round(parsedValue, 2);
                        
                return new MetricDataPoint
                {
                    Value = roundedValue
                };
            });
    }
}