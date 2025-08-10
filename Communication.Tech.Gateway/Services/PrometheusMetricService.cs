using System.Globalization;
using System.Text.Json;
using communication_tech.Helper;
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
    private readonly ILogger<PrometheusMetricService> _logger;

    public PrometheusMetricService(HttpClientService httpClientService, IOptions<PrometheusSettings> settings, ILogger<PrometheusMetricService> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
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
        
        var startUnix = TimeHelper.ConvertUtcToUnixTimeWithTurkeyTime(startTime);
        var endUnix = TimeHelper.ConvertUtcToUnixTimeWithTurkeyTime(endTime);

        
        var queryParams = new Dictionary<string, string>
        {
            { "query", query },
            { "start", startUnix.ToString() },
            { "end", endUnix.ToString() },
            { "step", step }
        };
        
        var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var fullUrl = $"{_prometheusSettings.Url}/{route}?{queryString}";
        _logger.LogInformation("Prometheus URL: {FullUrl}", fullUrl);
        _logger.LogInformation("Start time: {StartTime} -> Unix: {StartUnix}", startTime, startUnix);
        _logger.LogInformation("End time: {DateTime} -> Unix: {EndUnix}", endTime, endUnix);
        
        var response = await _httpClientService.GetAsyncWithQueryString<PrometheusQueryResponse>(
            _prometheusSettings.Url
            , route
            , queryParams);
        
        _logger.LogInformation("Response status: {ResponseStatus}", response?.Status);
        _logger.LogInformation("Result count: {ResultCount}", response?.Data?.Result?.Count ?? 0);
        
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