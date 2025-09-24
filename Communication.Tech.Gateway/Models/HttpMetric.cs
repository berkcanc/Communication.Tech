using communication_tech.Enums;

namespace communication_tech.Models;

public class HttpMetric : EnumBasedMetric
{
    public HttpMetric()
    {
        TechnologyType = TechnologyType.Http;
        ServiceName = "web-api";
    }
}