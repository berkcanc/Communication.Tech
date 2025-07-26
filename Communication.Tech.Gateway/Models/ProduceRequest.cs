namespace communication_tech.Models;

public class ProduceRequest
{
    public required string Message { get; set; }
    public int SizeInKB { get; set; }
}