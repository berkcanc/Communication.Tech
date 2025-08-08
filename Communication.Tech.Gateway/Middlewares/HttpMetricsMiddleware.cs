using System.Diagnostics;
using communication_tech.Interfaces;

namespace communication_tech.Middlewares;

public class HttpMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IPrometheusMetricService _metrics;

    public HttpMetricsMiddleware(RequestDelegate next, IPrometheusMetricService metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        List<string> excludePaths = new() { "swagger", "favicon", "/metrics", "produce", "grpc" };

        if (excludePaths.Any(p => 
                context.Request.Path.HasValue 
                && context.Request.Path.Value.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        _metrics.RecordHttpTurnaround(
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            context.Response.StatusCode,
            sw.Elapsed.TotalSeconds
        );
    }
}
