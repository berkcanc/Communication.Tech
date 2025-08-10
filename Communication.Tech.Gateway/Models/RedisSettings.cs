namespace communication_tech.Models;

public class RedisSettings(string connectionString)
{
    public string ConnectionString { get; set; } = connectionString;
    public bool IsEnabled { get; set; }
}
