using System.Text.Json;
using communication_tech.Enums;
using communication_tech.Interfaces;
using communication_tech.Models;

namespace communication_tech.Services;

public abstract class BaseMetricsCollector<T> : IMetricsCollector<T> where T : EnumBasedMetric
{
    private readonly HttpClient _httpClient;
    private readonly string _prometheusHost;
    protected readonly ILogger _logger;

    public abstract TechnologyType TechnologyType { get; }
    protected abstract string ThroughputQuery { get; }
    protected abstract string LatencyQuery { get; }
    protected abstract string ResponseTimeQuery { get; }
    protected abstract string TurnaroundTimeQuery { get; }

    protected BaseMetricsCollector(HttpClient httpClient, IConfiguration config, ILogger logger)
    {
        _httpClient = httpClient;
        _prometheusHost = config.GetValue<string>("Prometheus:Host", "http://127.0.0.1:9090");
        _logger = logger;
    }

    public abstract Task<T> CollectAsync();

    protected async Task<double> ExecuteSimpleQueryAsync(string query)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"{_prometheusHost}/api/v1/query?query={encodedQuery}";
            
            var response = await _httpClient.GetStringAsync(url);
            var prometheusResponse = JsonSerializer.Deserialize<PrometheusQueryResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var result = prometheusResponse?.Data?.Result?.FirstOrDefault();
            if (result is { Values.Count: > 1 })
            {
                return Convert.ToDouble(result.Values[1]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Query}", query);
        }
        
        return 0.0;
    }
}