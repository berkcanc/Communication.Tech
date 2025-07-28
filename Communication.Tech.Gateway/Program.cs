using System.Net;
using System.Net.Security;
using communication_tech.Interfaces;
using communication_tech.Middlewares;
using communication_tech.Models;
using communication_tech.Services;
using Communication.Tech.Protos;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Prometheus;
using StackExchange.Redis;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

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




builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"])
    );

// Services
builder.Services.AddSingleton<IPayloadGeneratorService, PayloadGeneratorService>();
builder.Services.AddSingleton<IPrometheusMetricService, PrometheusMetricService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<RabbitMQProducerService>();
builder.Services.AddSingleton<IRedisQueueService, RedisQueueService>();


builder.Services.AddControllers();
builder.Services.AddHttpClient<HttpClientService>();
builder.Services.AddScoped<HttpClientService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcMetricsInterceptor>();
});

builder.Services.AddGrpcClient<Greeter.GreeterClient>(options =>
{
    options.Address = new Uri(builder.Configuration["GeneralSettings:GrpcServerBaseAddress"]);
});

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