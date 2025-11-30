using System.Net.Sockets;

namespace communication_tech;

public class NoDelayTcpClient : RabbitMQ.Client.ITcpClient
{
    private readonly TcpClient _client;

    public NoDelayTcpClient(AddressFamily addressFamily)
    {
        _client = new TcpClient(addressFamily);
        _client.NoDelay = true; 
        
        _client.ReceiveBufferSize = 8192;
        _client.SendBufferSize = 8192;
    }

    public Socket Client => _client.Client;

    public NetworkStream GetStream()
    {
        return _client.GetStream();
    }

    public void Close()
    {
        _client.Close();
    }

    public bool Connected { get; }
    public TimeSpan ReceiveTimeout { get; set; }

    public Task ConnectAsync(string host, int port)
    {
        return _client.ConnectAsync(host, port);
    }
    
    public void Dispose()
    {
        _client.Dispose();
    }
}