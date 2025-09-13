namespace communication_tech.Models;

public class RabbitMqSettings
{
    public string? HostName { get; set; }
    public string? UserName { get; set; }
    public string Password { get; set; }
    public string? QueueName { get; set; }
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public bool IsEnabled { get; set; }
}