using Communication.Tech.Protos;
using Communication.Tech.Server;
using Communication.Tech.Server.Interfaces;
using Communication.Tech.Server.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    //HTTP
    options.ListenLocalhost(Constants.HttpServerPort, o =>
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
    });
    
    //gRPC
    options.ListenLocalhost(Constants.GrpcServerPort, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });
    
    //WebSocket
    options.ListenLocalhost(Constants.WebSocketServerPort, o =>
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

app.MapGrpcService<GreeterService>();
app.MapGraphQL();

app.MapControllers();

app.UseWebSockets();

app.Run();