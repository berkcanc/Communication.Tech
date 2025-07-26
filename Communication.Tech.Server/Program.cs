using communication_tech.Interfaces;
using communication_tech.Services;
using Communication.Tech.Server;
using Communication.Tech.Server.Interfaces;
using Communication.Tech.Server.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IGreeterService, GreeterService>();
builder.Services.AddSingleton<BookRepository>();
builder.Services.AddSingleton<WebSocketService>();
builder.Services.AddSingleton<IPayloadGeneratorService, PayloadGeneratorService>();


builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

builder.WebHost.ConfigureKestrel(options =>
{
    //gRPC
    options.ListenLocalhost(5010, o =>
    {
        o.Protocols = HttpProtocols.Http2;
        o.UseHttps();
    });
    
    //HTTP
    options.ListenLocalhost(5060, o =>
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
    });
    
    //HTTP2
    options.ListenLocalhost(6011, o =>
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
        o.UseHttps();
    });
    
    //WebSocket
    options.ListenLocalhost(5273, o =>
    {
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(60);
        // for inactive connection
        options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// WebSocket endpoint
app.Map("/ws", async context =>
{
    
    if (context.WebSockets.IsWebSocketRequest)
    {
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<WebSocketService>();
        await handler.HandleAsync(socket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.UseAuthorization();

app.MapControllers();

app.MapGrpcService<GreeterService>();
app.MapGrpcReflectionService();
app.MapGraphQL();

app.UseWebSockets();

app.Run();