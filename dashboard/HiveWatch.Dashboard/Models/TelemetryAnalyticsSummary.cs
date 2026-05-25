namespace HiveWatch.Dashboard.Models;

public sealed class TelemetryAnalyticsSummary
{
    public int ReadingCount { get; init; }

    public double? LatestValue { get; init; }

    public double? MinimumValue { get; init; }

    public double? MaximumValue { get; init; }

    public double? AverageValue { get; init; }

    public double? MedianValue { get; init; }

    public string Unit { get; init; } = "C";

    public string TrendStatus { get; init; } = "Not enough data";

    public string TrendMessage { get; init; } =
        "At least two readings are required before a trend can be calculated.";

    public string TrendBadgeClass { get; init; } = "bg-secondary text-white";

    public bool HasReadings => ReadingCount > 0;

    public static TelemetryAnalyticsSummary NoTelemetry() => new();
}
