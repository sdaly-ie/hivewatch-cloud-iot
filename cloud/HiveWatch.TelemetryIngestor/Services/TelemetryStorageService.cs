using Azure.Data.Tables;
using HiveWatch.TelemetryIngestor.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HiveWatch.TelemetryIngestor.Services;

public class TelemetryStorageService
{
    private const string DefaultTableName = "TelemetryReadings";

    private readonly TableClient _tableClient;
    private readonly ILogger<TelemetryStorageService> _logger;

    public TelemetryStorageService(
        IConfiguration configuration,
        ILogger<TelemetryStorageService> logger)
    {
        _logger = logger;

        string? connectionString = configuration["TelemetryStorageConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "TelemetryStorageConnectionString is not configured.");
        }

        string? configuredTableName = configuration["TelemetryTableName"];

        string tableName = string.IsNullOrWhiteSpace(configuredTableName)
            ? DefaultTableName
            : configuredTableName;

        _tableClient = new TableClient(connectionString, tableName);
    }

    public async Task StoreAsync(
        TelemetryReading reading,
        DateTimeOffset receivedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await _tableClient.CreateIfNotExistsAsync(cancellationToken);

        TelemetryTableEntity entity = new()
        {
            PartitionKey = reading.DeviceId!,
            RowKey = BuildRowKey(receivedAtUtc),
            DeviceId = reading.DeviceId!,
            SensorId = reading.SensorId!,
            Type = reading.Type!,
            Unit = reading.Unit!,
            Value = reading.Value!.Value,
            ReceivedAtUtc = receivedAtUtc
        };

        await _tableClient.AddEntityAsync(entity, cancellationToken);

        _logger.LogInformation(
            "Telemetry persisted to Azure Table Storage. PartitionKey={PartitionKey}, RowKey={RowKey}",
            entity.PartitionKey,
            entity.RowKey);
    }

    private static string BuildRowKey(DateTimeOffset receivedAtUtc)
    {
        string timestamp = receivedAtUtc.UtcDateTime.ToString("yyyyMMddHHmmssfffffff");
        return $"{timestamp}-{Guid.NewGuid():N}";
    }
}