using System.Text;
using communication_tech.Interfaces;

namespace communication_tech.Services;

public class PayloadGeneratorService : IPayloadGeneratorService
{
    public string GenerateMessage(string baseMessage, int sizeInKB)
    {
        if (sizeInKB <= 0)
            return baseMessage;

        int targetSizeInBytes = sizeInKB * 1024;
        int baseMessageSizeInBytes = Encoding.UTF8.GetByteCount(baseMessage);

        if (targetSizeInBytes <= baseMessageSizeInBytes)
            return baseMessage;

        // Padding karakteri (•) UTF-8’de 3 byte'tır.
        const string paddingChar = "•";
        var paddingCharByteSize = Encoding.UTF8.GetByteCount(paddingChar);

        var remainingBytes = targetSizeInBytes - baseMessageSizeInBytes;
        var paddingCount = remainingBytes / paddingCharByteSize;

        return baseMessage + string.Concat(Enumerable.Repeat(paddingChar, paddingCount));
    }
}
