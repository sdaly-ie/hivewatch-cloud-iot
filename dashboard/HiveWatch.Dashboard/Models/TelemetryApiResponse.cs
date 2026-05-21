namespace HiveWatch.Dashboard.Models;

public class TelemetryApiResponse
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }

    public List<TelemetryReadingRecord> Readings { get; set; } = new();
}
