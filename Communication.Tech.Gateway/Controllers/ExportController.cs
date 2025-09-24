using System.Globalization;
using System.Text;
using communication_tech.Enums;
using communication_tech.Helper;
using communication_tech.Interfaces;
using communication_tech.Models;
using Microsoft.AspNetCore.Mvc;

namespace communication_tech.Controllers;

[ApiController]
[Route("Data/export")]
public class ExportController : ControllerBase
{
    private readonly IPrometheusMetricService _prometheusService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IPrometheusMetricService prometheusService)
    {
        _prometheusService = prometheusService;
    }

    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportMetricsAsCsv(
        [FromQuery] string query,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] string step = "5s")
    {
        try
        {
            var (start, end) = TimeHelper.GetUtcStartEndFromTurkeyTime(startTime, endTime);

            var dataPoints = await _prometheusService.GetMetricRangeDataAsync(query, start, end, step);

            var metricDataPoints = dataPoints as MetricDataPoint[] ?? dataPoints.Where(p => p.Value != 0).ToArray();
            if (metricDataPoints.Length == 0)
                return NotFound("Data Not Found By Given Range.");

            var averageMetricValue = metricDataPoints
                .Average(x => x.Value)
                .ToString("0.00", CultureInfo.InvariantCulture);
            var builder = new StringBuilder();
            // CSV Header
            builder.AppendLine(query);
            builder.AppendLine("Average Metric Value");
            builder.AppendLine(averageMetricValue + " ms");

            var fileName = $"metrics_export_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    
    
    /// <summary>
    /// Collect metric for specific technology by ID (1=Http, 2=gRPC, 3=Redis, 4=RabbitMQ)
    /// </summary>
    [HttpPost("collect/{technologyId:int}")]
    public async Task<IActionResult> CollectSpecificMetric(int technologyId)
    {
        try
        {
            if (!IsValidTechnologyId(technologyId))
            {
                return BadRequest(new
                {
                    Message = $"Invalid technology ID: {technologyId}",
                    ValidIds = "1=Http, 2=gRPC, 3=Redis, 4=RabbitMQ",
                    Status = "ValidationError"
                });
            }

            var technologyType = (TechnologyType)technologyId;
            await _prometheusService.CollectAndStoreMetricsAsync(technologyType);

            return Ok(new
            {
                Message = $"Metric collected and stored for {technologyType}",
                TechnologyName = technologyType.ToString(),
                Status = "Success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting metric for technology ID {TechnologyId}", technologyId);
            return BadRequest(new
            {
                Message = "Error collecting metric",
                TechnologyId = technologyId,
                Error = ex.Message,
                Status = "Failed"
            });
        }
    }
    
    private static bool IsValidTechnologyId(int technologyId)
    {
        return Enum.IsDefined(typeof(TechnologyType), technologyId);
    }
}