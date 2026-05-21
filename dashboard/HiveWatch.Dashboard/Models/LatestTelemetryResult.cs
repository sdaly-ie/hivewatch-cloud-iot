namespace HiveWatch.Dashboard.Models;

public class LatestTelemetryResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public TelemetryReadingRecord? LatestReading { get; set; }
}
