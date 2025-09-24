using communication_tech.Enums;

namespace communication_tech.Models;

public abstract class EnumBasedMetric
{
    public TechnologyType TechnologyType { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Environment { get; set; } = string.Empty;
    public Dictionary<string, object> Tags { get; set; } = new();
    
    // Core 4 metrics
    public double Throughput { get; set; }
    public double Latency { get; set; }
    public double ResponseTime { get; set; }
    public double TurnaroundTime { get; set; }
}