using System.Globalization;
using System.Text;
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
}