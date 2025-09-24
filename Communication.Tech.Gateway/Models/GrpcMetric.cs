using communication_tech.Enums;

namespace communication_tech.Models;

public class GrpcMetric : EnumBasedMetric
{
    public GrpcMetric()
    {
        TechnologyType = TechnologyType.gRPC;
        ServiceName = "grpc-service";
    }
}