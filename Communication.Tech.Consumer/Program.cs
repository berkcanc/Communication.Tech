using communication_tech.Models;
using Communication.Tech.Consumer.Consumers;
using Communication.Tech.Consumer.Interfaces;
using Communication.Tech.Consumer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var redisConnectionString = configuration["Redis:ConnectionString"];
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IPrometheusConsumerMetricService, PrometheusConsumerMetricService>();

builder.Services.Configure<PrometheusSettings>(configuration.GetSection("Prometheus"));

var rabbitMqSettings = configuration.GetSection("RabbitMQ");
var rabbitMqSettingsModel = rabbitMqSettings.Get<RabbitMqSettings>();

if (rabbitMqSettingsModel is { IsEnabled: true })
{
    builder.Services.Configure<RabbitMqSettings>(rabbitMqSettings);
    builder.Services.AddHostedService<RabbitMQConsumer>();
}

var kafkaSettings = configuration.GetSection("Kafka");
var kafkaSettingsModel = kafkaSettings.Get<KafkaSettings>();

if (kafkaSettingsModel is { IsEnabled: true })
{
    builder.Services.Configure<KafkaSettings>(kafkaSettings);
    builder.Services.AddHostedService<KafkaConsumer>();
}

var redisSettings = configuration.GetSection("Redis");
var redisSettingsModel = redisSettings.Get<RedisSettings>();

if (redisSettingsModel is { IsEnabled: true })
{
    builder.Services.Configure<RedisSettings>(redisSettings);
    builder.Services.AddHostedService<RedisQueueConsumer>();
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

//Metrics.SuppressDefaultMetrics();     // ⛔ .NET default metrics disable
app.UseMetricServer(8080, "0.0.0.0");  // ✅ /metrics endpoint

app.Run();

