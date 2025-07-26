namespace communication_tech.Models;

public class RabbitMqSettings
{
    public string HostName { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string QueueName { get; set; } = default!;
    public bool IsEnabled { get; set; }
}