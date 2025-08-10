namespace communication_tech.Models;

public class KafkaSettings()
{
    public string? BootstrapServers { get; set; }
    public string? Topic { get; set; }
    public string? GroupId { get; set; } 
    public bool IsEnabled { get; set; }
}
