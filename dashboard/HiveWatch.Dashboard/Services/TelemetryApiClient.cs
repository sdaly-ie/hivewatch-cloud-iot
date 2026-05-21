using System.Text.Json;
using HiveWatch.Dashboard.Models;

namespace HiveWatch.Dashboard.Services;

public class TelemetryApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelemetryApiClient> _logger;

    public TelemetryApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TelemetryApiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LatestTelemetryResult> GetRecentReadingsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        string? endpoint = _configuration["TelemetryApi:RecentTelemetryUrl"];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new LatestTelemetryResult
            {
                Success = false,
                Message = "Telemetry retrieval endpoint is not configured yet."
            };
        }

        int safeLimit = Math.Clamp(limit, 1, 100);
        string separator = endpoint.Contains('?') ? "&" : "?";
        string requestUrl = $"{endpoint}{separator}limit={safeLimit}";

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(requestUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new LatestTelemetryResult
                {
                    Success = false,
                    Message = $"Telemetry API returned HTTP {(int)response.StatusCode}."
                };
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            TelemetryApiResponse? payload = await JsonSerializer.DeserializeAsync<TelemetryApiResponse>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                cancellationToken);

            List<TelemetryReadingRecord> readings = payload?.Readings ?? new();

            if (readings.Count == 0)
            {
                return new LatestTelemetryResult
                {
                    Success = false,
                    Message = "Telemetry API returned successfully, but no readings were found."
                };
            }

            return new LatestTelemetryResult
            {
                Success = true,
                Message = $"Retrieved {readings.Count} recent telemetry reading(s).",
                LatestReading = readings.FirstOrDefault(),
                RecentReadings = readings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard telemetry retrieval failed.");

            return new LatestTelemetryResult
            {
                Success = false,
                Message = "Dashboard could not retrieve telemetry from the configured endpoint."
            };
        }
    }
}
