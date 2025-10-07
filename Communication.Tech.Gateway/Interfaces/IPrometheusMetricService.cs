using communication_tech.Enums;
using communication_tech.Models;
using Grpc.Core;

namespace communication_tech.Interfaces;

public interface IPrometheusMetricService
{
    void RecordHttpTurnaround(string method, string path, int statusCode, double durationSeconds);
    void RecordGrpcThroughput(string service, string method, StatusCode statusCode, double durationSeconds);
    void RecordGrpcLatency(string service, string method, StatusCode statusCode, double durationSeconds);
    void RecordGrpcResponseTime(string service, string method, StatusCode statusCode, double durationSeconds);
    void RecordGrpcTurnaround(string service, string method, StatusCode statusCode, double durationSeconds);
    Task<IEnumerable<MetricDataPoint>> GetMetricRangeDataAsync(
        string query,
        DateTime startTime,
        DateTime endTime,
        string step);

    Task CollectAndStoreMetricsAsync(TechnologyType technologyType, int tps, int payloadSize);
}