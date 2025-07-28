using communication_tech.Interfaces;
using communication_tech.Models;
using communication_tech.Services;
using Communication.Tech.Consumer.Consumers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IPrometheusMetricService, PrometheusMetricService>();
builder.Services.AddSingleton<IRedisQueueService, RedisQueueService>();
builder.Services.AddSingleton<MessageStoreService>();

var configuration = builder.Configuration;


var rabbitMqSettings = configuration.GetSection("RabbitMQ");
var rabbitMqSettingsModel = rabbitMqSettings.Get<RabbitMqSettings>();

if (rabbitMqSettingsModel.IsEnabled)
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

if (kafkaSettingsModel.IsEnabled)
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

if (redisSettingsModel.IsEnabled)
{
    builder.Services.Configure<RedisSettings>(redisSettings);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(6379); // redis
    });
    builder.Services.AddHostedService<RedisQueueConsumer>();
}


var app = builder.Build();

Metrics.SuppressDefaultMetrics();     // ⛔ .NET default Meter'ları devre dışı bırak
app.UseMetricServer();               // ✅ /metrics endpoint
app.UseHttpMetrics();                // HTTP request metrikleri

app.Run();

