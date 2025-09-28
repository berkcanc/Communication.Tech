using communication_tech.Enums;

namespace communication_tech.Models;

public abstract class EnumBasedMetric
{
    public TechnologyType TechnologyType { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Environment { get; set; } = string.Empty;
    
    // Core 6 metrics
    public double Throughput { get; set; }
    public double Latency { get; set; }
    public double ResponseTime { get; set; }
    public double TurnaroundTime { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
}