using HiveWatch.Dashboard.Models;

namespace HiveWatch.Dashboard.Services;

public class BroodTemperatureAlertEvaluator
{
    private const int ConsecutiveReadingCountForSustainedAlert = 4;

    public BroodTemperatureAlertResult Evaluate(
        TelemetryReadingRecord? latestReading,
        IReadOnlyList<TelemetryReadingRecord> recentReadings)
    {
        if (latestReading is null)
        {
            return BroodTemperatureAlertResult.NoTelemetry();
        }

        BroodTemperatureAlertResult currentBand = ClassifyCurrentTemperature(latestReading.Value);

        List<TelemetryReadingRecord> recentTemperatureReadings = recentReadings
            .Where(reading => string.Equals(reading.Type, "temperature", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(reading => reading.ReceivedAtUtc)
            .Take(ConsecutiveReadingCountForSustainedAlert)
            .ToList();

        if (currentBand.OrderRank == 3)
        {
            return currentBand.WithSustainedEvaluation(
                sustainedStatus: "Not applicable",
                sustainedMessage: "Latest reading is within the reference brood range, so no sustained alert check is required.",
                sustainedAlertClass: "alert-success",
                isSustainedAlertConfirmed: false,
                readingsEvaluated: recentTemperatureReadings.Count,
                rollingAverage: recentTemperatureReadings.Count > 0
                    ? recentTemperatureReadings.Average(reading => reading.Value)
                    : null);
        }

        if (recentTemperatureReadings.Count < ConsecutiveReadingCountForSustainedAlert)
        {
            return currentBand.WithSustainedEvaluation(
                sustainedStatus: "Not enough data",
                sustainedMessage: $"Current reading is classified as {currentBand.AlertCategoryDescription}, but sustained confirmation needs 4 consecutive 10-minute readings or a 30-minute rolling average. Only {recentTemperatureReadings.Count} recent temperature reading(s) are available.",
                sustainedAlertClass: "alert-secondary",
                isSustainedAlertConfirmed: false,
                readingsEvaluated: recentTemperatureReadings.Count,
                rollingAverage: recentTemperatureReadings.Count > 0
                    ? recentTemperatureReadings.Average(reading => reading.Value)
                    : null);
        }

        double rollingAverage = recentTemperatureReadings.Average(reading => reading.Value);

        bool fourConsecutiveReadingsInCurrentBand = recentTemperatureReadings
            .All(reading => ClassifyCurrentTemperature(reading.Value).OrderRank == currentBand.OrderRank);

        bool rollingAverageInCurrentBand =
            ClassifyCurrentTemperature(rollingAverage).OrderRank == currentBand.OrderRank;

        if (fourConsecutiveReadingsInCurrentBand || rollingAverageInCurrentBand)
        {
            string basis = fourConsecutiveReadingsInCurrentBand
                ? "4 consecutive recent readings"
                : "30-minute rolling average";

            return currentBand.WithSustainedEvaluation(
                sustainedStatus: "Sustained alert confirmed",
                sustainedMessage: $"{basis} support this alert state. Rolling average: {rollingAverage:0.00} °C.",
                sustainedAlertClass: currentBand.BootstrapAlertClass,
                isSustainedAlertConfirmed: true,
                readingsEvaluated: recentTemperatureReadings.Count,
                rollingAverage: rollingAverage);
        }

        return currentBand.WithSustainedEvaluation(
            sustainedStatus: "Current deviation only",
            sustainedMessage: $"Latest reading is outside the reference range, but the recent readings do not yet confirm a sustained alert. Rolling average: {rollingAverage:0.00} °C.",
            sustainedAlertClass: "alert-secondary",
            isSustainedAlertConfirmed: false,
            readingsEvaluated: recentTemperatureReadings.Count,
            rollingAverage: rollingAverage);
    }

    private static BroodTemperatureAlertResult ClassifyCurrentTemperature(double temperatureCelsius)
    {
        if (temperatureCelsius < 33.0)
        {
            return new BroodTemperatureAlertResult
            {
                OrderRank = 1,
                AlertTrafficLight = "Red",
                AlertCategoryDescription = "Critical cold deviation",
                BroodAreaTemperatureBand = "< 33.0 °C",
                Meaning = "If this is genuinely brood-area temperature, it is below the regulated brood range and should be investigated. This is a risk signal, not proof that brood injury has occurred.",
                EvidenceConfidence = "Moderate-High",
                BootstrapAlertClass = "alert-danger",
                BootstrapBadgeClass = "text-bg-danger"
            };
        }

        if (temperatureCelsius < 34.0)
        {
            return new BroodTemperatureAlertResult
            {
                OrderRank = 2,
                AlertTrafficLight = "Amber",
                AlertCategoryDescription = "Cool brood warning",
                BroodAreaTemperatureBand = "33.0-33.9 °C",
                Meaning = "Below the preferred brood-centre target. Suitable as an early-warning band for investigation, especially if brood is present.",
                EvidenceConfidence = "Moderate",
                BootstrapAlertClass = "alert-warning",
                BootstrapBadgeClass = "text-bg-warning"
            };
        }

        if (temperatureCelsius <= 36.0)
        {
            return new BroodTemperatureAlertResult
            {
                OrderRank = 3,
                AlertTrafficLight = "Green",
                AlertCategoryDescription = "Reference brood range",
                BroodAreaTemperatureBand = "34.0-36.0 °C",
                Meaning = "Within the expected operational brood-area range used by the HiveWatch proof of concept. No action required from this signal alone.",
                EvidenceConfidence = "High",
                BootstrapAlertClass = "alert-success",
                BootstrapBadgeClass = "text-bg-success"
            };
        }

        if (temperatureCelsius <= 37.0)
        {
            return new BroodTemperatureAlertResult
            {
                OrderRank = 4,
                AlertTrafficLight = "Amber",
                AlertCategoryDescription = "Warm brood warning",
                BroodAreaTemperatureBand = "36.1-37.0 °C",
                Meaning = "Above the usual brood-centre target. This is an operational caution band suggesting rising thermoregulatory load.",
                EvidenceConfidence = "Low-Moderate",
                BootstrapAlertClass = "alert-warning",
                BootstrapBadgeClass = "text-bg-warning"
            };
        }

        return new BroodTemperatureAlertResult
        {
            OrderRank = 5,
            AlertTrafficLight = "Red",
            AlertCategoryDescription = "Critical heat deviation",
            BroodAreaTemperatureBand = "> 37.0 °C",
            Meaning = "Above the regulated brood range. Conservatively flagged as a critical alert for investigation if sustained.",
            EvidenceConfidence = "Moderate",
            BootstrapAlertClass = "alert-danger",
            BootstrapBadgeClass = "text-bg-danger"
        };
    }
}
