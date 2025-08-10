using System.Text.Json.Serialization;

namespace communication_tech.Models;

public class PrometheusData
{
    [JsonPropertyName("resultType")]
    public string? ResultType { get; set; }

    [JsonPropertyName("result")]
    public List<PrometheusResult> Result { get; set; }
}