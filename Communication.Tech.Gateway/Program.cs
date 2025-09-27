using communication_tech.Interfaces;
using communication_tech.Middlewares;
using communication_tech.Models;
using communication_tech.Services;
using Communication.Tech.Gateway.Protos;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Prometheus;
using StackExchange.Redis;
using Constants = communication_tech.Constants;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var configuration = builder.Configuration;

builder.Services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMQ"));

var redisConnectionString = configuration["Redis:ConnectionString"];
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.Configure<PrometheusSettings>(configuration.GetSection("Prometheus"));

builder.WebHost.ConfigureKestrel(options =>
{
    // Global timeout ayarları
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);         // TCP bağlantısı açık kalma süresi
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(60);    // Header'ların alınması için bekleme süresi
    options.Limits.MinRequestBodyDataRate = null;                        // Body veri hızı sınırlamasını kaldır
    options.Limits.MinResponseDataRate = null;
    
    //Kafka, RabbitMQ, GraphQL Trigger, Redis
    options.ListenAnyIP(Constants.HttpGatewayPort, o =>
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
    });
    
    // gRPC
    options.ListenAnyIP(Constants.GrpcGatewayPort, o =>
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
    });
});





builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcMetricsInterceptor>();
});
builder.Services.AddHttpClient<HttpClientService>();
builder.Services.AddSingleton<HttpClientService>();

// Services
builder.Services.AddSingleton<IPayloadGeneratorService, PayloadGeneratorService>();

builder.Services.AddHttpClient();

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

app.UseMetricServer();

app.UseWebSockets();
app.UseMiddleware<HttpMetricsMiddleware>();


app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();
//app.UseAuthorization();
app.MapControllers();

app.Run();