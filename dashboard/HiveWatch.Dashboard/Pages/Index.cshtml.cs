using HiveWatch.Dashboard.Models;
using HiveWatch.Dashboard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HiveWatch.Dashboard.Pages;

public class IndexModel : PageModel
{
    private readonly TelemetryApiClient _telemetryApiClient;

    public IndexModel(TelemetryApiClient telemetryApiClient)
    {
        _telemetryApiClient = telemetryApiClient;
    }

    public TelemetryReadingRecord? LatestReading { get; private set; }

    public string StatusMessage { get; private set; } = "Telemetry has not been checked yet.";

    public bool HasError { get; private set; }

    public async Task OnGetAsync()
    {
        LatestTelemetryResult result =
            await _telemetryApiClient.GetLatestReadingAsync(HttpContext.RequestAborted);

        LatestReading = result.LatestReading;
        StatusMessage = result.Message;
        HasError = !result.Success;
    }
}
