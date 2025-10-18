using communication_tech.Enums;

namespace communication_tech.Models;

public class KafkaMetric : EnumBasedMetric
{
    public KafkaMetric()
    {
        TechnologyType = TechnologyType.Kafka;
        ServiceName = "kafka-broker";
    }
}