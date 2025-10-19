using System.Reflection;
using communication_tech.Enums;
using communication_tech.Interfaces;
using communication_tech.Models;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Prometheus;

namespace communication_tech.Services;

public class PrometheusMetricService : IPrometheusMetricService
{
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
    
    private static readonly Counter _grpcRequestCounter = Metrics.CreateCounter(
        "grpc_client_requests_total",
        "Total gRPC requests made from client",
        new CounterConfiguration
        {
            LabelNames = new[] { "grpc_service", "grpc_method", "status_code" }
        }
    );
    
    private static readonly Histogram _grpcLatencyHistogram = Metrics.CreateHistogram(
        "grpc_client_latency_seconds",
        "End-to-end gRPC request latency from client perspective",
        new HistogramConfiguration
        {
            Buckets = new double[] {0.001,0.005,0.01,0.05,0.1,0.5,1,2,5},
            LabelNames = new[] { "grpc_service", "grpc_method", "status_code" }
        }
    );
    
    private static readonly Histogram _grpcResponseTimeHistogram = Metrics.CreateHistogram(
        "grpc_client_response_time_seconds",
        "gRPC response time from client perspective",
        new HistogramConfiguration
        {
            Buckets = new double[] {0.001,0.005,0.01,0.05,0.1,0.5,1,2,5},
            LabelNames = new[] { "grpc_service", "grpc_method", "status_code" }
        }
    );

    private static readonly Histogram _redisLatency = Metrics.CreateHistogram(
        "redis_producer_command_duration_seconds",
        "Redis command latency in seconds (producer side)",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.0001, 2, 15), // 0.1ms to ~3s
            LabelNames = new[] { "command" }
        });

    private static readonly Histogram _redisResponseTime = Metrics.CreateHistogram(
        "redis_producer_response_time_seconds",
        "Redis operation response time in seconds (producer side)",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.0001, 2, 15),
            LabelNames = new[] { "operation" }
        });
    
    private static readonly Histogram _kafkaLatencyHistogram = Metrics.CreateHistogram(
        "kafka_producer_latency_seconds",
        "Time taken for Kafka producer to send a message and receive acknowledgment",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 15), // 1ms - 16s
            LabelNames = new[] { "command" }
        });

    private static readonly Histogram _kafkaResponseTimeHistogram = Metrics.CreateHistogram(
        "kafka_producer_response_time_seconds",
        "End-to-end Kafka producer response time including local processing",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 15),
            LabelNames = new[] { "operation" }
        });
    
    private readonly HttpClientService _httpClientService;
    private readonly PrometheusSettings _prometheusSettings;
    private readonly ILogger<PrometheusMetricService> _logger;
    private readonly IMetricsFileStorageService _metricsFileStorageService;
    private readonly IServiceProvider _serviceProvider;

    public PrometheusMetricService(HttpClientService httpClientService, IOptions<PrometheusSettings> settings, ILogger<PrometheusMetricService> logger, IMetricsFileStorageService metricsFileStorageService, IServiceProvider serviceProvider)
    {
        _httpClientService = httpClientService;
        _logger = logger;
        _metricsFileStorageService = metricsFileStorageService;
        _serviceProvider = serviceProvider;
        _prometheusSettings = settings.Value;
    }

    public void RecordHttpTurnaround(string method, string path, int statusCode, double durationSeconds)
    {
        _httpHistogram.WithLabels(method, path.ToLower(), statusCode.ToString()).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Turnaround metric: {method}, {path}, {statusCode} = {durationSeconds:F3}s");
    }

    public void RecordGrpcTurnaround(string service, string method, StatusCode statusCode, double durationSeconds)
    {
        _grpcHistogram.WithLabels(service, method, statusCode.ToString()).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed gRPC Turnaround metric: {method}, {method}, {statusCode} = {durationSeconds:F3}s");
    }
    
    public void RecordGrpcThroughput(string service, string method, StatusCode statusCode, double durationSeconds)
    {
        _grpcRequestCounter.WithLabels(service, method, statusCode.ToString()).Inc();
        Console.WriteLine($"📊 Observed gRPC Throughput metric: {method}, {method}, {statusCode} = {durationSeconds:F3}s");
    }
    
    public void RecordGrpcLatency(string service, string method, StatusCode statusCode, double durationSeconds)
    {
        _grpcLatencyHistogram.WithLabels(service, method, statusCode.ToString()).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed gRPC Latency metric: {method}, {method}, {statusCode} = {durationSeconds:F3}s");
    }
    
    public void RecordGrpcResponseTime(string service, string method, StatusCode statusCode, double durationSeconds)
    {
        _grpcResponseTimeHistogram .WithLabels(service, method, statusCode.ToString()).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed gRPC Response Time metric: {service}, {method}, {statusCode} = {durationSeconds:F3}s");
    }
    
    public void RecordRedisLatency(string command, double durationSeconds)
    {
        _redisLatency.WithLabels(command).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Redis Latency metric: {command}, {durationSeconds:F3}s");
    }

    public void RecordRedisResponseTime(string operation, double durationSeconds)
    {
        _redisResponseTime.WithLabels(operation).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Redis Response Time metric: {operation}, {durationSeconds:F3}s");
    }
    
    public void RecordKafkaLatency(string command, double durationSeconds)
    {
        _kafkaLatencyHistogram.WithLabels(command).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Kafka Latency metric: {command}, {durationSeconds:F3}s");
    }

    public void RecordKafkaResponseTime(string operation, double durationSeconds)
    {
        _kafkaResponseTimeHistogram.WithLabels(operation).Observe(durationSeconds);
        Console.WriteLine($"📊 Observed Kafka Response Time metric: {operation}, {durationSeconds:F3}s");
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

        return [];
        /*return response.Data.Result
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
            });*/
    }
    
    public async Task CollectAndStoreMetricsAsync(TechnologyType technologyType, int tps, int payloadSize)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Starting metrics collection for {Technology} at {StartTime}",
            technologyType, startTime
        );

        try
        {
            var metric = await CollectMetricForTechnologyAsync(technologyType);
            if (metric == null)
            {
                _logger.LogWarning("No metric collected for {Technology}", technologyType);
                return;
            }

            await _metricsFileStorageService.SaveMetricsAsync(metric, tps, payloadSize);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Completed metrics collection for {Technology} in {Duration}ms. Metric saved to file",
                technologyType, duration.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting metric for {Technology}", technologyType);
            throw;
        }
    }


    private async Task<EnumBasedMetric?> CollectMetricForTechnologyAsync(TechnologyType technologyType)
    {
        // TechnologyType -> EnumBasedMetric mapping
        var typeMap = new Dictionary<TechnologyType, Type>
        {
            { TechnologyType.Http, typeof(HttpMetric) },
            { TechnologyType.gRPC, typeof(GrpcMetric) },
            { TechnologyType.Redis, typeof(RedisMetric) },
            { TechnologyType.RabbitMQ, typeof(RabbitMqMetric) },
            { TechnologyType.Kafka, typeof(KafkaMetric) }
        };

        if (!typeMap.TryGetValue(technologyType, out var metricType))
            return null;

        // CollectMetricAsync<T> method call with reflection
        var methodInfo = typeof(PrometheusMetricService)
            .GetMethod(nameof(CollectMetricAsync), BindingFlags.Instance | BindingFlags.Public)!
            .MakeGenericMethod(metricType);

        var task = (Task)methodInfo.Invoke(this, null)!;
        var result = await (dynamic)task;

        return (EnumBasedMetric)result;
    }

    public async Task<T> CollectMetricAsync<T>() where T : EnumBasedMetric
    {
        try
        {
            var collector = _serviceProvider.GetRequiredService<IMetricsCollector<T>>();
            var metric = await collector.CollectAsync();

            _logger.LogInformation("Collected single {TechnologyType} metric", metric.TechnologyType);
            
            return metric;
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}