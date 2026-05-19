using Azure;
using Azure.Data.Tables;

namespace HiveWatch.TelemetryIngestor.Models;

public class TelemetryTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public string SensorId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public double Value { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }
}