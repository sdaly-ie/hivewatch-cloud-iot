namespace HiveWatch.Dashboard.Models;

public class BroodTemperatureAlertResult
{
    public int? OrderRank { get; init; }

    public string AlertTrafficLight { get; init; } = "Grey";

    public string AlertCategoryDescription { get; init; } = "No telemetry";

    public string BroodAreaTemperatureBand { get; init; } = "Not available";

    public string SustainedDefinition { get; init; } =
        "30-minute rolling average, or 4 consecutive 10-minute readings.";

    public string Meaning { get; init; } =
        "No brood-area temperature reading is currently available.";

    public string EvidenceConfidence { get; init; } = "Not applicable";

    public string BootstrapAlertClass { get; init; } = "alert-secondary";

    public string BootstrapBadgeClass { get; init; } = "text-bg-secondary";

    public string SustainedStatus { get; init; } = "Not evaluated";

    public string SustainedMessage { get; init; } =
        "No telemetry is available for sustained-alert evaluation.";

    public string SustainedAlertClass { get; init; } = "alert-secondary";

    public bool IsSustainedAlertConfirmed { get; init; }

    public int ReadingsEvaluated { get; init; }

    public double? RollingAverage { get; init; }

    public static BroodTemperatureAlertResult NoTelemetry()
    {
        return new BroodTemperatureAlertResult();
    }

    public BroodTemperatureAlertResult WithSustainedEvaluation(
        string sustainedStatus,
        string sustainedMessage,
        string sustainedAlertClass,
        bool isSustainedAlertConfirmed,
        int readingsEvaluated,
        double? rollingAverage)
    {
        return new BroodTemperatureAlertResult
        {
            OrderRank = OrderRank,
            AlertTrafficLight = AlertTrafficLight,
            AlertCategoryDescription = AlertCategoryDescription,
            BroodAreaTemperatureBand = BroodAreaTemperatureBand,
            SustainedDefinition = SustainedDefinition,
            Meaning = Meaning,
            EvidenceConfidence = EvidenceConfidence,
            BootstrapAlertClass = BootstrapAlertClass,
            BootstrapBadgeClass = BootstrapBadgeClass,
            SustainedStatus = sustainedStatus,
            SustainedMessage = sustainedMessage,
            SustainedAlertClass = sustainedAlertClass,
            IsSustainedAlertConfirmed = isSustainedAlertConfirmed,
            ReadingsEvaluated = readingsEvaluated,
            RollingAverage = rollingAverage
        };
    }
}
