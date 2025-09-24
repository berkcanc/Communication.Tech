using communication_tech.Models;

namespace communication_tech.Interfaces;

public interface IMetricsFileStorageService
{
    Task SaveMetricsAsync(EnumBasedMetric metricInfo);
}