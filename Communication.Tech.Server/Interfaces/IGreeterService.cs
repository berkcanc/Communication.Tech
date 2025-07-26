using Grpc.Core;

namespace Communication.Tech.Server.Interfaces;

public interface IGreeterService
{
    Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context);
}