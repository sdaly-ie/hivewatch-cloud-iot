using System.Globalization;
using HiveWatch.Dashboard.Models;

namespace HiveWatch.Dashboard.Services;

public sealed class TelemetryAnalyticsCalculator
{
    private const double StableTrendThresholdCelsius = 0.20;

    public TelemetryAnalyticsSummary Calculate(IReadOnlyList<TelemetryReadingRecord> readings)
    {
        List<TelemetryReadingRecord> temperatureReadings = readings
            .Where(reading => string.Equals(reading.Type, "temperature", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(reading => reading.ReceivedAtUtc)
            .ToList();

        if (temperatureReadings.Count == 0)
        {
            return TelemetryAnalyticsSummary.NoTelemetry();
        }

        double[] values = temperatureReadings
            .Select(reading => Convert.ToDouble(reading.Value, CultureInfo.InvariantCulture))
            .OrderBy(value => value)
            .ToArray();

        TelemetryReadingRecord latestReading = temperatureReadings[0];
        double latestValue = Convert.ToDouble(latestReading.Value, CultureInfo.InvariantCulture);

        return new TelemetryAnalyticsSummary
        {
            ReadingCount = temperatureReadings.Count,
            LatestValue = latestValue,
            MinimumValue = values.First(),
            MaximumValue = values.Last(),
            AverageValue = values.Average(),
            MedianValue = CalculateMedian(values),
            Unit = string.IsNullOrWhiteSpace(latestReading.Unit) ? "C" : latestReading.Unit,
            TrendStatus = CalculateTrendStatus(temperatureReadings),
            TrendMessage = CalculateTrendMessage(temperatureReadings),
            TrendBadgeClass = CalculateTrendBadgeClass(temperatureReadings)
        };
    }

    private static double CalculateMedian(double[] orderedValues)
    {
        int middleIndex = orderedValues.Length / 2;

        if (orderedValues.Length % 2 == 1)
        {
            return orderedValues[middleIndex];
        }

        return (orderedValues[middleIndex - 1] + orderedValues[middleIndex]) / 2.0;
    }

    private static string CalculateTrendStatus(IReadOnlyList<TelemetryReadingRecord> readings)
    {
        if (readings.Count < 2)
        {
            return "Not enough data";
        }

        double delta = GetLatestDelta(readings);

        if (Math.Abs(delta) <= StableTrendThresholdCelsius)
        {
            return "Stable";
        }

        return delta > 0 ? "Rising" : "Falling";
    }

    private static string CalculateTrendMessage(IReadOnlyList<TelemetryReadingRecord> readings)
    {
        if (readings.Count < 2)
        {
            return "At least two readings are required before a trend can be calculated.";
        }

        double delta = GetLatestDelta(readings);
        double absoluteDelta = Math.Abs(delta);

        if (absoluteDelta <= StableTrendThresholdCelsius)
        {
            return $"Latest reading changed by {absoluteDelta:0.00} C compared with the previous reading, so the trend is treated as stable.";
        }

        string direction = delta > 0 ? "higher" : "lower";
        return $"Latest reading is {absoluteDelta:0.00} C {direction} than the previous reading.";
    }

    private static string CalculateTrendBadgeClass(IReadOnlyList<TelemetryReadingRecord> readings)
    {
        if (readings.Count < 2)
        {
            return "bg-secondary text-white";
        }

        double delta = GetLatestDelta(readings);

        if (Math.Abs(delta) <= StableTrendThresholdCelsius)
        {
            return "bg-success text-white";
        }

        return delta > 0 ? "bg-warning text-dark" : "bg-info text-dark";
    }

    private static double GetLatestDelta(IReadOnlyList<TelemetryReadingRecord> readings)
    {
        double latest = Convert.ToDouble(readings[0].Value, CultureInfo.InvariantCulture);
        double previous = Convert.ToDouble(readings[1].Value, CultureInfo.InvariantCulture);

        return latest - previous;
    }
}
