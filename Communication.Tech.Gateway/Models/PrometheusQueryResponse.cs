using System.Text.Json.Serialization;

namespace communication_tech.Models;

public class PrometheusQueryResponse
{
    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("data")]
    public required PrometheusData Data { get; set; }
}