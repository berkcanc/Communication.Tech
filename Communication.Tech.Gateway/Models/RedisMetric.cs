using communication_tech.Enums;

namespace communication_tech.Models;

public class RedisMetric : EnumBasedMetric
{
    public RedisMetric()
    {
        TechnologyType = TechnologyType.Redis;
        ServiceName = "redis-cache";
    }
}