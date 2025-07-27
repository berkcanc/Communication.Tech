namespace Communication.Tech.Server.Interfaces;

public interface IPayloadGeneratorService
{
    string GenerateMessage(string baseMessage, int sizeInBytes);
}