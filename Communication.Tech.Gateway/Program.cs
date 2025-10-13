using communication_tech.Interfaces;
using communication_tech.Middlewares;
using communication_tech.Models;
using communication_tech.Services;
using Communication.Tech.Gateway.Protos;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Prometheus;
using StackExchange.Redis;
using Constants = communication_tech.Constants;
using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using HistogramConfiguration = Prometheus.HistogramConfiguration;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// for gRPC
/*builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("GrpcGateway"))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddGrpcClientInstrumentation(); 
    });*/


// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var configuration = builder.Configuration;

// RabbitMQ
builder.Services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMQ"));

// Redis
var redisConnectionString = configuration["Redis:ConnectionString"];
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));

// Prometheus settings
builder.Services.Configure<PrometheusSettings>(configuration.GetSection("Prometheus"));

// Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(60);
    options.Limits.MinRequestBodyDataRate = null;
    options.Limits.MinResponseDataRate = null;

    options.ListenAnyIP(Constants.HttpGatewayPort, o => o.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenAnyIP(Constants.GrpcGatewayPort, o => o.Protocols = HttpProtocols.Http1AndHttp2);
});

// gRPC
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcMetricsInterceptor>());

// HttpClient services
builder.Services.AddHttpClient<HttpClientService>();
builder.Services.AddSingleton<HttpClientService>();

// Other services
builder.Services.AddSingleton<IPayloadGeneratorService, PayloadGeneratorService>();
builder.Services.AddSingleton<IMetricsCollector<HttpMetric>, HttpMetricsCollector>();
builder.Services.AddSingleton<IMetricsCollector<GrpcMetric>, GrpcMetricsCollector>();
builder.Services.AddSingleton<IMetricsCollector<RedisMetric>, RedisMetricsCollector>();
builder.Services.AddSingleton<IMetricsCollector<RabbitMqMetric>, RabbitMqMetricsCollector>();
builder.Services.AddSingleton<IMetricsFileStorageService, MetricsFileStorageService>();
builder.Services.AddSingleton<IPrometheusMetricService, PrometheusMetricService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<RabbitMQProducerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<GrpcMetricsInterceptor>();

builder.Services.AddGrpcClient<Greeter.GreeterClient>(options =>
{
    options.Address = new Uri(configuration["GeneralSettings:GrpcServerBaseAddress"] ?? string.Empty);
}).AddInterceptor<GrpcMetricsInterceptor>();

builder.Services.AddGrpcReflection();

var app = builder.Build();

//app.MapPrometheusScrapingEndpoint(); for gRPC

// Prometheus metric server
app.UseMetricServer();

// --- Bucket HTTP response time histogram ---
var httpResponseHistogram = Metrics.CreateHistogram(
    "http_request_duration_seconds",
    "HTTP response time in seconds",
    new HistogramConfiguration
    {
        Buckets =
        [
            0.001,0.002,0.004,0.008,0.016,0.032,0.064,0.128,0.256,0.512,1,2,4,8,16,32
        ],
        LabelNames = ["method", "status_code", "route"]
    }
);

// Middleware for measuring response time
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    httpResponseHistogram
        .WithLabels(context.Request.Method, context.Response.StatusCode.ToString(), context.Request.Path)
        .Observe(sw.Elapsed.TotalSeconds);
});

// --- Turnaround time middleware ---
app.UseMiddleware<HttpMetricsMiddleware>();

// gRPC, WebSockets
app.UseHttpMetrics();
app.UseWebSockets();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Communication.Tech.Gateway v1");
    c.RoutePrefix = "swagger";
});

// Routing
app.MapControllers();

app.Run();
