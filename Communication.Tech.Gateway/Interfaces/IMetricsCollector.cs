using communication_tech.Enums;
using communication_tech.Models;

namespace communication_tech.Interfaces;

public interface IMetricsCollector<T> where T : EnumBasedMetric
{
    Task<T> CollectAsync();
    TechnologyType TechnologyType { get; }
}