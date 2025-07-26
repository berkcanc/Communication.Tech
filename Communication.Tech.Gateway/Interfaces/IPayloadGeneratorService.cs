namespace communication_tech.Interfaces;

public interface IPayloadGeneratorService
{
    string GenerateMessage(string baseMessage, int sizeInBytes);
}