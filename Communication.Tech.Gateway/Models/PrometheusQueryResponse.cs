using System.Text.Json.Serialization;

namespace communication_tech.Models;

public class PrometheusQueryResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("data")]
    public PrometheusData Data { get; set; }
}

public class PrometheusData
{
    [JsonPropertyName("resultType")]
    public string ResultType { get; set; }

    [JsonPropertyName("result")]
    public List<PrometheusResult> Result { get; set; }
}

public class PrometheusResult
{
    // "metric" alanı { "label1": "value1", ... } gibi olduğu için dictionary olarak alıyoruz
    [JsonPropertyName("metric")]
    public Dictionary<string, string> Metric { get; set; }

    // "values" alanı [ [timestamp, "value"], [timestamp, "value"] ] formatındadır.
    // System.Text.Json bunu doğrudan List<object> olarak okuyabilir.
    [JsonPropertyName("values")]
    public List<List<object>> Values { get; set; }
}

public class MetricDataPoint
{
    public decimal Value { get; set; }
}