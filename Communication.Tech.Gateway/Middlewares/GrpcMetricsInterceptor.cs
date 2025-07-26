using System.Diagnostics;
using communication_tech.Interfaces;

namespace communication_tech.Middlewares;

using Grpc.Core;
using Grpc.Core.Interceptors;

public class GrpcMetricsInterceptor : Interceptor
{
    private readonly IPrometheusMetricService _metrics;

    public GrpcMetricsInterceptor(IPrometheusMetricService metrics)
    {
        _metrics = metrics;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, 
        ServerCallContext context, 
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await continuation(request, context);
            sw.Stop();
            _metrics.RecordGrpcTurnaround(
                context.Method.Split('/')[1], // service
                context.Method.Split('/')[2], // method
                context.Status.StatusCode,
                sw.Elapsed.TotalSeconds
            );
            return response;
        }
        catch (Exception)
        {
            sw.Stop();
            _metrics.RecordGrpcTurnaround(
                context.Method.Split('/')[1],
                context.Method.Split('/')[2],
                context.Status.StatusCode,
                sw.Elapsed.TotalSeconds
            );
            throw;
        }
    }
}
