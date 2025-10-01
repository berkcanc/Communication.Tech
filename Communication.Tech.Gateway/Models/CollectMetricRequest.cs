namespace communication_tech.Models;

public class CollectMetricRequest
{
    public int TechnologyId { get; set; }
    public int Tps { get; set; }
    public int PayloadSize { get; set; }
}