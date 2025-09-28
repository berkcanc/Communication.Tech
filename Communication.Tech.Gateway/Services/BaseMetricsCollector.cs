using System.Text.Json;
using communication_tech.Enums;
using communication_tech.Interfaces;
using communication_tech.Models;
using Microsoft.Extensions.Options;

namespace communication_tech.Services;

public abstract class BaseMetricsCollector<T> : IMetricsCollector<T> where T : EnumBasedMetric
{
    private readonly HttpClient _httpClient;
    private readonly PrometheusSettings _prometheusSettings;
    protected readonly ILogger _logger;

    public abstract TechnologyType TechnologyType { get; }
    protected abstract string ThroughputQuery { get; }
    protected abstract string LatencyQuery { get; }
    protected abstract string ResponseTimeQuery { get; }
    protected abstract string TurnaroundTimeQuery { get; }
    protected string CpuUsageQuery => 
        "100 - (avg by (instance) (rate(node_cpu_seconds_total{mode=\"idle\"}[5m])) * 100)";

    protected string MemoryUsageQuery => 
        "100 * ((node_memory_active_bytes + node_memory_wired_bytes + node_memory_compressed_bytes) / node_memory_total_bytes)";

    protected BaseMetricsCollector(HttpClient httpClient, IConfiguration config, ILogger logger, IOptions<PrometheusSettings> settings)
    {
        _httpClient = httpClient;
        _prometheusSettings = settings.Value;
        _logger = logger;
    }

    public abstract Task<T> CollectAsync();

    protected async Task<double> ExecuteSimpleQueryAsync(string query)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"{_prometheusSettings.Url}/api/v1/query?query={encodedQuery}";
            
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