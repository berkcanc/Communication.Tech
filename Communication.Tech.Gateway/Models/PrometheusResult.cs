using System.Text.Json.Serialization;

namespace communication_tech.Models;

public class PrometheusResult
{
    // "metric" => { "label1": "value1", ... } 
    [JsonPropertyName("metric")]
    public Dictionary<string, string>? Metric { get; set; }

    // "values" => [ [timestamp, "value"], [timestamp, "value"] ].
    [JsonPropertyName("value")]
    public object[] Value { get; set; }
}

public class MetricDataPoint
{
    public decimal Value { get; set; }
}