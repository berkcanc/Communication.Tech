using System.Text.Json.Serialization;

namespace communication_tech.Models;

public class PrometheusQueryResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("data")]
    public PrometheusData Data { get; set; }
}