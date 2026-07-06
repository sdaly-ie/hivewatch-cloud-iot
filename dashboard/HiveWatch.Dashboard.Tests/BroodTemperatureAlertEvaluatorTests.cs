using HiveWatch.Dashboard.Models;
using HiveWatch.Dashboard.Services;

namespace HiveWatch.Dashboard.Tests;

public sealed class BroodTemperatureAlertEvaluatorTests
{
    private static readonly DateTimeOffset BaseTime =
        new(2026, 7, 6, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_WhenLatestReadingIsNull_ReturnsNoTelemetryResult()
    {
        BroodTemperatureAlertEvaluator evaluator = new();

        BroodTemperatureAlertResult result = evaluator.Evaluate(null, []);

        Assert.Equal("Grey", result.AlertTrafficLight);
        Assert.Equal("No telemetry", result.AlertCategoryDescription);
        Assert.False(result.IsSustainedAlertConfirmed);
        Assert.Equal(0, result.ReadingsEvaluated);
        Assert.Null(result.RollingAverage);
    }

    [Fact]
    public void Evaluate_WhenLatestReadingIsInReferenceRange_DoesNotRequireSustainedAlertCheck()
    {
        BroodTemperatureAlertEvaluator evaluator = new();

        TelemetryReadingRecord latestReading = CreateReading(35.0, minutesAgo: 0);
        List<TelemetryReadingRecord> recentReadings =
        [
            latestReading,
            CreateReading(34.8, minutesAgo: 10),
            CreateReading(35.1, minutesAgo: 20),
            CreateReading(35.2, minutesAgo: 30)
        ];

        BroodTemperatureAlertResult result = evaluator.Evaluate(latestReading, recentReadings);

        Assert.Equal(3, result.OrderRank);
        Assert.Equal("Green", result.AlertTrafficLight);
        Assert.Equal("Reference brood range", result.AlertCategoryDescription);
        Assert.Equal("Not applicable", result.SustainedStatus);
        Assert.False(result.IsSustainedAlertConfirmed);
        Assert.Equal(4, result.ReadingsEvaluated);
    }

    [Fact]
    public void Evaluate_WhenColdDeviationHasTooFewReadings_DoesNotConfirmSustainedAlert()
    {
        BroodTemperatureAlertEvaluator evaluator = new();

        TelemetryReadingRecord latestReading = CreateReading(32.5, minutesAgo: 0);
        List<TelemetryReadingRecord> recentReadings =
        [
            latestReading,
            CreateReading(32.7, minutesAgo: 10)
        ];

        BroodTemperatureAlertResult result = evaluator.Evaluate(latestReading, recentReadings);

        Assert.Equal(1, result.OrderRank);
        Assert.Equal("Red", result.AlertTrafficLight);
        Assert.Equal("Critical cold deviation", result.AlertCategoryDescription);
        Assert.Equal("Not enough data", result.SustainedStatus);
        Assert.False(result.IsSustainedAlertConfirmed);
        Assert.Equal(2, result.ReadingsEvaluated);
    }

    [Fact]
    public void Evaluate_WhenFourRecentReadingsAreInCurrentAlertBand_ConfirmsSustainedAlert()
    {
        BroodTemperatureAlertEvaluator evaluator = new();

        TelemetryReadingRecord latestReading = CreateReading(32.5, minutesAgo: 0);
        List<TelemetryReadingRecord> recentReadings =
        [
            latestReading,
            CreateReading(32.6, minutesAgo: 10),
            CreateReading(32.7, minutesAgo: 20),
            CreateReading(32.8, minutesAgo: 30)
        ];

        BroodTemperatureAlertResult result = evaluator.Evaluate(latestReading, recentReadings);

        Assert.Equal(1, result.OrderRank);
        Assert.Equal("Critical cold deviation", result.AlertCategoryDescription);
        Assert.Equal("Sustained alert confirmed", result.SustainedStatus);
        Assert.True(result.IsSustainedAlertConfirmed);
        Assert.Equal(4, result.ReadingsEvaluated);
        Assert.Equal(32.65, result.RollingAverage!.Value, precision: 2);
    }

    private static TelemetryReadingRecord CreateReading(double value, int minutesAgo)
    {
        return new TelemetryReadingRecord
        {
            DeviceId = "hivewatch-esp32-board2",
            SensorId = "ds18b20-1",
            Type = "temperature",
            Unit = "C",
            Value = value,
            ReceivedAtUtc = BaseTime.AddMinutes(-minutesAgo)
        };
    }
}