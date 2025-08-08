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

    #region Client-Side Interceptors

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        var call = continuation(request, context);
        
        return new AsyncUnaryCall<TResponse>(
            HandleClientUnaryResponse(call.ResponseAsync
                , context.Method.ServiceName
                , context.Method.Name
                , sw),
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
            HandleClientUnaryResponse(call.ResponseAsync
                , context.Method.ServiceName
                , context.Method.Name
                , sw),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose
        );
    }

    #endregion
    
    private async Task<TResponse> HandleClientUnaryResponse<TResponse>(
        Task<TResponse> responseTask, 
        string serviceName,
        string methodName,
        Stopwatch sw)
    {
        try
        {
            var response = await responseTask;
            sw.Stop();
            
            // Success case - client tarafında method bilgisi farklı şekilde alınır
            _metrics.RecordGrpcTurnaround(
                serviceName, // service
                methodName, // method
                StatusCode.OK,
                sw.Elapsed.TotalSeconds
            );
            
            return response;
        }
        catch (RpcException rpcEx)
        {
            sw.Stop();
            
            // gRPC error case
            _metrics.RecordGrpcTurnaround(
                serviceName,
                methodName,
                rpcEx.StatusCode,
                sw.Elapsed.TotalSeconds
            );
            
            throw;
        }
        catch (Exception)
        {
            sw.Stop();
            
            // Other error case
            _metrics.RecordGrpcTurnaround(
                serviceName,
                methodName,
                StatusCode.Internal,
                sw.Elapsed.TotalSeconds
            );
            
            throw;
        }
    }
}
