using System.Text.Json;
using HiveWatch.TelemetryIngestor.Models;
using HiveWatch.TelemetryIngestor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWatch.TelemetryIngestor;

public class Function1
{
    private const int DefaultRecentReadingLimit = 20;

    private readonly ILogger<Function1> _logger;
    private readonly TelemetryStorageService _telemetryStorageService;

    public Function1(
        ILogger<Function1> logger,
        TelemetryStorageService telemetryStorageService)
    {
        _logger = logger;
        _telemetryStorageService = telemetryStorageService;
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

        try
        {
            await _telemetryStorageService.StoreAsync(
                reading,
                receivedAtUtc,
                req.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Validated telemetry could not be persisted. DeviceId={DeviceId}, SensorId={SensorId}",
                reading.DeviceId,
                reading.SensorId);

            return new ObjectResult(new
            {
                status = "error",
                message = "Telemetry payload was valid, but storage failed."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        _logger.LogInformation(
            "Telemetry accepted and persisted: DeviceId={DeviceId}, SensorId={SensorId}, Type={Type}, Unit={Unit}, Value={Value}, ReceivedAtUtc={ReceivedAtUtc}",
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

    [Function("GetRecentTelemetry")]
    public async Task<IActionResult> GetRecentTelemetry(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        _logger.LogInformation("HiveWatch recent telemetry retrieval request received.");

        int requestedLimit = DefaultRecentReadingLimit;
        string limitText = req.Query["limit"].ToString();

        if (!string.IsNullOrWhiteSpace(limitText))
        {
            if (!int.TryParse(limitText, out requestedLimit) || requestedLimit < 1)
            {
                return new BadRequestObjectResult(new
                {
                    status = "rejected",
                    message = "The limit query parameter must be a positive whole number."
                });
            }
        }

        try
        {
            IReadOnlyList<TelemetryReadingRecord> readings =
                await _telemetryStorageService.GetRecentAsync(
                    requestedLimit,
                    req.HttpContext.RequestAborted);

            return new OkObjectResult(new
            {
                status = "ok",
                count = readings.Count,
                readings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recent telemetry retrieval failed.");

            return new ObjectResult(new
            {
                status = "error",
                message = "Stored telemetry could not be retrieved."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
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
}
