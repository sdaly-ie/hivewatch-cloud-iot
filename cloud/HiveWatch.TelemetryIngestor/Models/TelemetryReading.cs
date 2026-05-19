using System.Text.Json.Serialization;

namespace HiveWatch.TelemetryIngestor.Models;

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