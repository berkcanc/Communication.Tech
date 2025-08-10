using communication_tech.Interfaces;
using communication_tech.Models;
using communication_tech.Services;
using Communication.Tech.Consumer.Consumers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var redisConnectionString = configuration["Redis:ConnectionString"];
if (redisConnectionString != null)
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<HttpClientService>();
builder.Services.AddSingleton<HttpClientService>();
builder.Services.AddSingleton<IPrometheusMetricService, PrometheusMetricService>();
builder.Services.AddSingleton<IRedisQueueService, RedisQueueService>();
builder.Services.AddSingleton<MessageStoreService>();

builder.Services.Configure<PrometheusSettings>(configuration.GetSection("Prometheus"));
var rabbitMqSettings = configuration.GetSection("RabbitMQ");
var rabbitMqSettingsModel = rabbitMqSettings.Get<RabbitMqSettings>();

if (rabbitMqSettingsModel is { IsEnabled: true })
{
    builder.Services.Configure<RabbitMqSettings>(rabbitMqSettings);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(15692); // rabbitmq
    });
    builder.Services.AddHostedService<RabbitMQConsumer>();
}

var kafkaSettings = configuration.GetSection("Kafka");
var kafkaSettingsModel = kafkaSettings.Get<KafkaSettings>();

if (kafkaSettingsModel is { IsEnabled: true })
{
    builder.Services.Configure<KafkaSettings>(kafkaSettings);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(9092); // kafka
    });
    builder.Services.AddHostedService<KafkaConsumer>();
}

var redisSettings = configuration.GetSection("Redis");
var redisSettingsModel = redisSettings.Get<RedisSettings>();

if (redisSettingsModel is { IsEnabled: true })
{
    builder.Services.Configure<RedisSettings>(redisSettings);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(6379); // redis
    });
    builder.Services.AddHostedService<RedisQueueConsumer>();
}


var app = builder.Build();

Metrics.SuppressDefaultMetrics();     // ⛔ .NET default metrics disable
app.UseMetricServer();               // ✅ /metrics endpoint
app.UseHttpMetrics();                // HTTP request metrics

app.Run();

