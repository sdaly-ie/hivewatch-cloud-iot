namespace HiveWatch.TelemetryIngestor.Models;

public class TelemetryReadingRecord
{
    public string DeviceId { get; set; } = string.Empty;

    public string SensorId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public double Value { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }
}
