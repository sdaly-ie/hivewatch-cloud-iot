using HiveWatch.Dashboard.Models;
using HiveWatch.Dashboard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HiveWatch.Dashboard.Pages;

public class IndexModel : PageModel
{
    private const int RecentReadingLimit = 10;
    private static readonly TimeSpan FreshTelemetryThreshold = TimeSpan.FromHours(1);

    private readonly TelemetryApiClient _telemetryApiClient;

    public IndexModel(TelemetryApiClient telemetryApiClient)
    {
        _telemetryApiClient = telemetryApiClient;
    }

    public TelemetryReadingRecord? LatestReading { get; private set; }

    public IReadOnlyList<TelemetryReadingRecord> RecentReadings { get; private set; } =
        Array.Empty<TelemetryReadingRecord>();

    public string StatusMessage { get; private set; } = "Telemetry has not been checked yet.";

    public bool HasError { get; private set; }

    public string FreshnessStatus { get; private set; } = "No telemetry";

    public string FreshnessMessage { get; private set; } = "No telemetry reading is currently available.";

    public string FreshnessAlertClass { get; private set; } = "alert-secondary";

    public async Task OnGetAsync()
    {
        LatestTelemetryResult result =
            await _telemetryApiClient.GetRecentReadingsAsync(
                RecentReadingLimit,
                HttpContext.RequestAborted);

        LatestReading = result.LatestReading;
        RecentReadings = result.RecentReadings;
        StatusMessage = result.Message;
        HasError = !result.Success;

        UpdateFreshnessStatus();
    }

    private void UpdateFreshnessStatus()
    {
        if (LatestReading is null)
        {
            FreshnessStatus = "No telemetry";
            FreshnessMessage = "No telemetry reading is currently available.";
            FreshnessAlertClass = "alert-secondary";
            return;
        }

        TimeSpan age = DateTimeOffset.UtcNow - LatestReading.ReceivedAtUtc;

        if (age <= FreshTelemetryThreshold)
        {
            FreshnessStatus = "Fresh";
            FreshnessMessage = $"Latest reading received {FormatAge(age)} ago.";
            FreshnessAlertClass = "alert-success";
            return;
        }

        FreshnessStatus = "Stale";
        FreshnessMessage = $"Latest reading is {FormatAge(age)} old. Device may be offline or no recent telemetry has been posted.";
        FreshnessAlertClass = "alert-warning";
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalMinutes < 1)
        {
            return "less than one minute";
        }

        if (age.TotalHours < 1)
        {
            int minutes = Math.Max(1, (int)Math.Floor(age.TotalMinutes));
            return $"{minutes} minute(s)";
        }

        if (age.TotalDays < 1)
        {
            int hours = Math.Max(1, (int)Math.Floor(age.TotalHours));
            return $"{hours} hour(s) {age.Minutes} minute(s)";
        }

        int days = Math.Max(1, (int)Math.Floor(age.TotalDays));
        return $"{days} day(s) {age.Hours} hour(s)";
    }
}
