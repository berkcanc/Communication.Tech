using System.Net.WebSockets;
using System.Text;
using Communication.Tech.Server.Interfaces;

namespace Communication.Tech.Server.Services;

public class WebSocketService
{
    private readonly IPayloadGeneratorService _payloadGeneratorService;

    public WebSocketService(IPayloadGeneratorService payloadGeneratorService)
    {
        _payloadGeneratorService = payloadGeneratorService;
    }

    public async Task HandleAsync(WebSocket socket)
    {
        var buffer = new byte[1024 * 4];

        while (true)
        {
            try
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("❌ WebSocket connection closed.");
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                // message = "text|sizeInKB"
                var parts = message.Split('|');
                var text = parts.Length > 0 ? parts[0] : "default";
                var size = parts.Length > 1 && int.TryParse(parts[1], out var s) ? s : 1;

                var payload = _payloadGeneratorService.GenerateMessage(text, size);

                // First 100 character previews
                var echoMsg = $"Payload generated: {payload.Substring(0, Math.Min(payload.Length, 100))}..."; 
                var response = Encoding.UTF8.GetBytes(echoMsg);
                await socket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("✅ Message sent: " + echoMsg);
            }
            catch (WebSocketException websocketException)
            {
                if (socket.State is WebSocketState.Open
                    or WebSocketState.CloseReceived
                    or WebSocketState.CloseSent)
                {
                    Console.WriteLine("❌ WebSocket connection closed unexpectedly: " + websocketException.Message);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing after error", CancellationToken.None);
                }

                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                break;
            }
        }
    }
}