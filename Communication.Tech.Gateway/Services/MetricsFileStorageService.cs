using System.Text.Json;
using System.Text.Json.Serialization;
using communication_tech.Helper;
using communication_tech.Interfaces;
using communication_tech.Models;

namespace communication_tech.Services;

public class MetricsFileStorageService : IMetricsFileStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<MetricsFileStorageService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MetricsFileStorageService(IConfiguration config, ILogger<MetricsFileStorageService> logger)
    {
        _storagePath = "/app/results";
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
    
    public async Task SaveMetricsAsync(EnumBasedMetric metricInfo)
    {
        try
        {
            EnsureDirectoryExists();
            
            var timestamp = DateTime.UtcNow;
            var timestampStr = TimeHelper.GetTurkeyTimestamp(timestamp);
            var shortGuid = Guid.NewGuid().ToString("N")[..3];

            var fileName = $"{metricInfo.TechnologyType}_{timestampStr}_{shortGuid}.json";
            var filePath = Path.Combine(_storagePath, fileName);

            var json = JsonSerializer.Serialize(metricInfo, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation(
                "Saved metrics to file: {FileName} at {Timestamp}", fileName, timestamp
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving metrics collection to file");
            throw;
        }
    }
    private void EnsureDirectoryExists()
    {
        if (Directory.Exists(_storagePath)) return;
        
        Directory.CreateDirectory(_storagePath);
        _logger.LogInformation("Created metrics storage directory: {StoragePath}", _storagePath);
    }
}