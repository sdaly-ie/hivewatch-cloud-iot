using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWatch.TelemetryIngestor;

public class Function1
{
    private readonly ILogger<Function1> _logger;

    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
    }

    [Function("IngestTelemetry")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("HiveWatch telemetry ingestion request received.");

        TelemetryReading? reading;

        try
        {
            reading = await JsonSerializer.DeserializeAsync<TelemetryReading>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON telemetry payload received.");

            return new BadRequestObjectResult(new
            {
                status = "rejected",
                message = "Invalid JSON payload."
            });
        }

        if (reading is null)
        {
            return new BadRequestObjectResult(new
            {
                status = "rejected",
                message = "Request body was empty or could not be read."
            });
        }

        List<string> validationErrors = Validate(reading);

        if (validationErrors.Count > 0)
        {
            return new BadRequestObjectResult(new
            {
                status = "rejected",
                message = "Telemetry payload failed validation.",
                errors = validationErrors
            });
        }

        DateTimeOffset receivedAtUtc = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Telemetry accepted: DeviceId={DeviceId}, SensorId={SensorId}, Type={Type}, Unit={Unit}, Value={Value}, ReceivedAtUtc={ReceivedAtUtc}",
            reading.DeviceId,
            reading.SensorId,
            reading.Type,
            reading.Unit,
            reading.Value,
            receivedAtUtc);

        return new OkObjectResult(new
        {
            status = "accepted",
            received_at_utc = receivedAtUtc,
            telemetry = reading
        });
    }

    private static List<string> Validate(TelemetryReading reading)
    {
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(reading.DeviceId))
        {
            errors.Add("device_id is required.");
        }

        if (string.IsNullOrWhiteSpace(reading.SensorId))
        {
            errors.Add("sensor_id is required.");
        }

        if (string.IsNullOrWhiteSpace(reading.Type))
        {
            errors.Add("type is required.");
        }

        if (string.IsNullOrWhiteSpace(reading.Unit))
        {
            errors.Add("unit is required.");
        }

        if (reading.Value is null)
        {
            errors.Add("value is required.");
        }

        return errors;
    }

    public class TelemetryReading
    {
        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("sensor_id")]
        public string? SensorId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("value")]
        public double? Value { get; set; }
    }
}