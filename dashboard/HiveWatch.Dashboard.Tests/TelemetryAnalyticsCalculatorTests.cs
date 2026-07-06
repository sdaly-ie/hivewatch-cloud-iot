using HiveWatch.Dashboard.Models;
using HiveWatch.Dashboard.Services;

namespace HiveWatch.Dashboard.Tests;

public sealed class TelemetryAnalyticsCalculatorTests
{
    private static readonly DateTimeOffset BaseTime =
        new(2026, 7, 6, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_WhenNoTemperatureReadingsExist_ReturnsNoTelemetrySummary()
    {
        TelemetryAnalyticsCalculator calculator = new();

        TelemetryAnalyticsSummary result = calculator.Calculate(
        [
            CreateReading(55.0, minutesAgo: 0, type: "humidity", unit: "%")
        ]);

        Assert.False(result.HasReadings);
        Assert.Equal(0, result.ReadingCount);
        Assert.Null(result.LatestValue);
        Assert.Null(result.MinimumValue);
        Assert.Null(result.MaximumValue);
        Assert.Null(result.AverageValue);
        Assert.Null(result.MedianValue);
        Assert.Equal("Not enough data", result.TrendStatus);
    }

    [Fact]
    public void Calculate_WithTemperatureReadings_ReturnsExpectedStatisticsAndRisingTrend()
    {
        TelemetryAnalyticsCalculator calculator = new();

        TelemetryAnalyticsSummary result = calculator.Calculate(
        [
            CreateReading(23.12, minutesAgo: 0),
            CreateReading(21.25, minutesAgo: 10),
            CreateReading(19.05, minutesAgo: 20),
            CreateReading(18.42, minutesAgo: 30)
        ]);

        Assert.True(result.HasReadings);
        Assert.Equal(4, result.ReadingCount);
        Assert.Equal(23.12, result.LatestValue);
        Assert.Equal(18.42, result.MinimumValue);
        Assert.Equal(23.12, result.MaximumValue);
        Assert.Equal(20.46, result.AverageValue!.Value, precision: 2);
        Assert.Equal(20.15, result.MedianValue!.Value, precision: 2);
        Assert.Equal("C", result.Unit);
        Assert.Equal("Rising", result.TrendStatus);
        Assert.Equal("bg-warning text-dark", result.TrendBadgeClass);
    }

    [Fact]
    public void Calculate_WhenLatestDeltaIsWithinThreshold_ReturnsStableTrend()
    {
        TelemetryAnalyticsCalculator calculator = new();

        TelemetryAnalyticsSummary result = calculator.Calculate(
        [
            CreateReading(20.10, minutesAgo: 0),
            CreateReading(20.00, minutesAgo: 10)
        ]);

        Assert.Equal("Stable", result.TrendStatus);
        Assert.Equal("bg-success text-white", result.TrendBadgeClass);
    }

    [Fact]
    public void Calculate_WhenLatestReadingIsLowerThanPreviousReading_ReturnsFallingTrend()
    {
        TelemetryAnalyticsCalculator calculator = new();

        TelemetryAnalyticsSummary result = calculator.Calculate(
        [
            CreateReading(19.70, minutesAgo: 0),
            CreateReading(20.10, minutesAgo: 10)
        ]);

        Assert.Equal("Falling", result.TrendStatus);
        Assert.Equal("bg-info text-dark", result.TrendBadgeClass);
    }

    private static TelemetryReadingRecord CreateReading(
        double value,
        int minutesAgo,
        string type = "temperature",
        string unit = "C")
    {
        return new TelemetryReadingRecord
        {
            DeviceId = "hivewatch-esp32-board2",
            SensorId = "ds18b20-1",
            Type = type,
            Unit = unit,
            Value = value,
            ReceivedAtUtc = BaseTime.AddMinutes(-minutesAgo)
        };
    }
}