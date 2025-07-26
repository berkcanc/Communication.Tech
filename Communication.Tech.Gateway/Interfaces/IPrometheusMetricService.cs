using Grpc.Core;

namespace communication_tech.Interfaces;

public interface IPrometheusMetricService
{
    void RecordMessageQueueTurnaround(string messageId, string messageType, string source, double durationSeconds);
    void RecordHttpTurnaround(string method, string path, int statusCode, double durationSeconds);
    void RecordGrpcTurnaround(string service, string method, StatusCode statusCode, double durationSeconds);
}