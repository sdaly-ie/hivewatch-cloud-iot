#include <WiFi.h>
#include <HTTPClient.h>
#include <NetworkClientSecure.h>
#include <OneWire.h>
#include <DallasTemperature.h>

// Wi-Fi credentials
const char* ssid = "YOUR_WIFI_SSID";
const char* password = "YOUR_WIFI_PASSWORD";

// Hosted Azure Function endpoint placeholder
const char* endpointUrl = "https://YOUR-AZURE-FUNCTION-ENDPOINT/api/IngestTelemetry";

// DS18B20 setup
#define ONE_WIRE_BUS 4  // ESP32 D4

OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature sensors(&oneWire);

// Device metadata for JSON payload
const char* deviceId = "hivewatch-esp32-board2";
const char* sensorId = "ds18b20-1";

void connectToWiFi() {
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);

  Serial.print("Connecting to Wi-Fi");

  int attempts = 0;
  const int maxAttempts = 30;

  while (WiFi.status() != WL_CONNECTED && attempts < maxAttempts) {
    delay(1000);
    Serial.print(".");
    attempts++;
  }

  Serial.println();

  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("Wi-Fi connected successfully.");
    Serial.print("IP address: ");
    Serial.println(WiFi.localIP());

    Serial.print("Signal strength (RSSI): ");
    Serial.print(WiFi.RSSI());
    Serial.println(" dBm");
  } else {
    Serial.println("Wi-Fi connection failed.");
  }
}

void postTemperatureToHostedAzureFunction(float temperatureC) {
  NetworkClientSecure secureClient;

  // Temporary HTTPS shortcut for this smoke test.
  secureClient.setInsecure();

  HTTPClient https;

  Serial.println();
  Serial.println("Preparing JSON payload...");

  String payload =
    "{"
    "\"device_id\":\"" + String(deviceId) + "\","
    "\"sensor_id\":\"" + String(sensorId) + "\","
    "\"type\":\"temperature\","
    "\"unit\":\"C\","
    "\"value\":" + String(temperatureC, 2) +
    "}";

  Serial.print("Payload: ");
  Serial.println(payload);

  Serial.println("Sending HTTPS POST to hosted HiveWatch Azure Function...");

  if (https.begin(secureClient, endpointUrl)) {
    https.addHeader("Content-Type", "application/json");

    int httpResponseCode = https.POST(payload);

    if (httpResponseCode > 0) {
      Serial.print("HTTP response code: ");
      Serial.println(httpResponseCode);

      String responseBody = https.getString();
      Serial.print("Response body: ");
      Serial.println(responseBody);
    } else {
      Serial.print("HTTPS POST failed. Error code: ");
      Serial.println(httpResponseCode);
    }

    https.end();
  } else {
    Serial.println("Unable to initialise HTTPS connection.");
  }
}

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println();
  Serial.println("Starting HiveWatch one-shot temperature POST test...");

  connectToWiFi();

  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("Stopping: Wi-Fi is not connected.");
    return;
  }

  sensors.begin();

  Serial.print("Number of temperature devices found: ");
  Serial.println(sensors.getDeviceCount());

  sensors.requestTemperatures();
  float temperatureC = sensors.getTempCByIndex(0);

  if (temperatureC == DEVICE_DISCONNECTED_C) {
    Serial.println("Stopping: DS18B20 did not return a valid temperature.");
    return;
  }

  Serial.print("Temperature captured: ");
  Serial.print(temperatureC);
  Serial.println(" °C");

  postTemperatureToHostedAzureFunction(temperatureC);

  Serial.println();
  Serial.println("One-shot POST test complete.");
}

void loop() {
  // Intentionally empty.
  // This proof-of-concept sends one temperature reading only.
}

