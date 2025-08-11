using communication_tech.Models;
using Grpc.Core;

namespace communication_tech.Interfaces;

public interface IPrometheusMetricService
{
    void RecordHttpTurnaround(string method, string path, int statusCode, double durationSeconds);
    void RecordGrpcTurnaround(string service, string method, StatusCode statusCode, double durationSeconds);
    Task<IEnumerable<MetricDataPoint>> GetMetricRangeDataAsync(
        string query,
        DateTime startTime,
        DateTime endTime,
        string step);
}