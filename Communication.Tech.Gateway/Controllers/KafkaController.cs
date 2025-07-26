using communication_tech.Interfaces;
using communication_tech.Models;
using communication_tech.Services;
using Microsoft.AspNetCore.Mvc;

namespace communication_tech.Controllers;

[ApiController]
[Route("[controller]")]
public class KafkaController : ControllerBase
{
    private readonly KafkaProducerService _kafkaProducerService;
    private readonly IPayloadGeneratorService _payloadGeneratorService;

    public KafkaController(KafkaProducerService kafkaProducerService, IPayloadGeneratorService payloadGeneratorService)
    {
        _kafkaProducerService = kafkaProducerService;
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
        await _kafkaProducerService.ProduceAsync(payload);
        return Ok("Message sent to Kafka.");
    }
}

