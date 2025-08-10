namespace communication_tech.Models;

public class ApiResponse(string message)
{
    public string Message { get; set; } = message;
}