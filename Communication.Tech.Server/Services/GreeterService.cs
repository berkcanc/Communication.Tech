using Communication.Tech.Server.Interfaces;
using Grpc.Core;

namespace Communication.Tech.Server.Services;

public class GreeterService : Greeter.GreeterBase, IGreeterService
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        var response = new HelloReply { Message = $"{request.Name}" };
        return Task.FromResult(response);
    }
}