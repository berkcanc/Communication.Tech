namespace communication_tech.Models;

public class ApiRequest(string message)
{
    public string Message { get; set; } = message;
}