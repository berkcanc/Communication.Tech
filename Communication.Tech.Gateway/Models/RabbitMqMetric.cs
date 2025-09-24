using communication_tech.Enums;

namespace communication_tech.Models;

public class RabbitMqMetric : EnumBasedMetric
{
    public RabbitMqMetric()
    {
        TechnologyType = TechnologyType.RabbitMQ;
        ServiceName = "rabbitmq-broker";
    }
}