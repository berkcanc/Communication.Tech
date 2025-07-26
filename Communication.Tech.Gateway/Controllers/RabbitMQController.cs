using communication_tech.Interfaces;
using communication_tech.Models;
using communication_tech.Services;
using Microsoft.AspNetCore.Mvc;

namespace communication_tech.Controllers;

[ApiController]
[Route("[controller]")]
public class RabbitMQController : Controller
{
    private readonly RabbitMQProducerService _rabbitMQProducerService;
    private readonly IPayloadGeneratorService _payloadGeneratorService;

    public RabbitMQController(RabbitMQProducerService rabbitMQProducerService, IPayloadGeneratorService payloadGeneratorService)
    {
        _rabbitMQProducerService = rabbitMQProducerService;
        _payloadGeneratorService = payloadGeneratorService;
    }
    
    [HttpPost("produce")]
    public async Task<IActionResult> ProduceMessage([FromBody] ProduceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty.");
        }

        var payload = _payloadGeneratorService.GenerateMessage(request.Message, request.SizeInKB);
        await _rabbitMQProducerService.SendMessageAsync(payload);
        return Ok("Message sent to RabbitMQConsumer.");
    }
}