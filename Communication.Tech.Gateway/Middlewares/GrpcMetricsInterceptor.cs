using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using communication_tech.Interfaces;

namespace communication_tech.Middlewares;

public class GrpcMetricsInterceptor : Interceptor
{
    private readonly IPrometheusMetricService _metrics;

    public GrpcMetricsInterceptor(IPrometheusMetricService metrics)
    {
        _metrics = metrics;
    }

    #region Client-Side Interceptors

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            HandleResponse(call.ResponseAsync, context.Method.ServiceName, context.Method.Name, sw),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose
        );
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        var call = continuation(context);

        return new AsyncClientStreamingCall<TRequest, TResponse>(
            call.RequestStream,
            HandleResponse(call.ResponseAsync, context.Method.ServiceName, context.Method.Name, sw),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose
        );
    }

    #endregion

    private async Task<TResponse> HandleResponse<TResponse>(
        Task<TResponse> responseTask,
        string serviceName,
        string methodName,
        Stopwatch sw)
    {
        try
        {
            var response = await responseTask;
            sw.Stop();

            // Client-side metrics
            RecordMetrics(serviceName, methodName, sw);

            return response;
        }
        catch (RpcException rpcEx)
        {
            sw.Stop();
            RecordMetrics(serviceName, methodName, sw);
            throw;
        }
        catch (Exception)
        {
            sw.Stop();
            RecordMetrics(serviceName, methodName, sw);
            throw;
        }
    }

    private void RecordMetrics(string serviceName, string methodName, Stopwatch sw)
    {
        _metrics.RecordGrpcResponseTime(serviceName, methodName, StatusCode.OK, sw.Elapsed.TotalSeconds);
        _metrics.RecordGrpcLatency(serviceName, methodName, StatusCode.OK, sw.Elapsed.TotalSeconds);
        _metrics.RecordGrpcThroughput(serviceName, methodName, StatusCode.OK, sw.Elapsed.TotalSeconds);
        _metrics.RecordGrpcTurnaround(serviceName, methodName, StatusCode.OK, sw.Elapsed.TotalSeconds);
    }
}