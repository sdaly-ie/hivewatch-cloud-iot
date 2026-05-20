using System.Linq;
using Azure.Data.Tables;
using HiveWatch.TelemetryIngestor.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HiveWatch.TelemetryIngestor.Services;

public class TelemetryStorageService
{
    private const string DefaultTableName = "TelemetryReadings";
    private const int MaximumRecentReadingLimit = 100;

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

    public async Task<IReadOnlyList<TelemetryReadingRecord>> GetRecentAsync(
        int requestedLimit,
        CancellationToken cancellationToken = default)
    {
        await _tableClient.CreateIfNotExistsAsync(cancellationToken);

        int safeLimit = Math.Clamp(
            requestedLimit,
            1,
            MaximumRecentReadingLimit);

        List<TelemetryReadingRecord> readings = new();

        await foreach (TelemetryTableEntity entity in _tableClient.QueryAsync<TelemetryTableEntity>(
            cancellationToken: cancellationToken))
        {
            readings.Add(new TelemetryReadingRecord
            {
                DeviceId = entity.DeviceId,
                SensorId = entity.SensorId,
                Type = entity.Type,
                Unit = entity.Unit,
                Value = entity.Value,
                ReceivedAtUtc = entity.ReceivedAtUtc
            });
        }

        IReadOnlyList<TelemetryReadingRecord> recentReadings = readings
            .OrderByDescending(reading => reading.ReceivedAtUtc)
            .Take(safeLimit)
            .ToList();

        _logger.LogInformation(
            "Telemetry retrieval completed. RequestedLimit={RequestedLimit}, ReturnedCount={ReturnedCount}",
            requestedLimit,
            recentReadings.Count);

        return recentReadings;
    }

    private static string BuildRowKey(DateTimeOffset receivedAtUtc)
    {
        string timestamp = receivedAtUtc.UtcDateTime.ToString("yyyyMMddHHmmssfffffff");
        return $"{timestamp}-{Guid.NewGuid():N}";
    }
}
