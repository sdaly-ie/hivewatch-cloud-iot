#include <WiFi.h>
#include <HTTPClient.h>
#include <NetworkClientSecure.h>
#include <OneWire.h>
#include <DallasTemperature.h>


#include "arduino_secrets.h"

// DS18B20 setup.
#define ONE_WIRE_BUS 4  // ESP32 D4

OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature sensors(&oneWire);

// Device metadata for JSON payload.
const char* deviceId = "hivewatch-esp32-board2";
const char* sensorId = "ds18b20-1";

const unsigned long TELEMETRY_INTERVAL_MS = 5UL * 60UL * 1000UL;

unsigned long lastTelemetryAttemptMs = 0;
unsigned long successfulPostCount = 0;
unsigned long failedPostCount = 0;

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

    Serial.print("Signal strength RSSI: ");
    Serial.print(WiFi.RSSI());
    Serial.println(" dBm");
  } else {
    Serial.println("Wi-Fi connection failed.");
  }
}

bool ensureWiFiConnected() {
  if (WiFi.status() == WL_CONNECTED) {
    return true;
  }

  Serial.println("Wi-Fi is not connected. Attempting reconnect...");
  connectToWiFi();

  return WiFi.status() == WL_CONNECTED;
}

bool postTemperatureToHostedAzureFunction(float temperatureC) {
  NetworkClientSecure secureClient;

  // Test sketch only: skip certificate validation.
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

  if (!https.begin(secureClient, endpointUrl)) {
    Serial.println("Unable to initialise HTTPS connection.");
    return false;
  }

  https.addHeader("Content-Type", "application/json");

  int httpResponseCode = https.POST(payload);

  bool accepted = false;

  if (httpResponseCode > 0) {
    Serial.print("HTTP response code: ");
    Serial.println(httpResponseCode);

    String responseBody = https.getString();
    Serial.print("Response body: ");
    Serial.println(responseBody);

    accepted = (httpResponseCode >= 200 && httpResponseCode < 300);
  } else {
    Serial.print("HTTPS POST failed. Error code: ");
    Serial.println(httpResponseCode);
  }

  https.end();

  return accepted;
}

void sendTelemetryAttempt() {
  Serial.println();
  Serial.println("Starting sustained telemetry attempt...");

  if (!ensureWiFiConnected()) {
    failedPostCount++;
    Serial.println("Telemetry attempt skipped: Wi-Fi unavailable.");
    printRunCounters();
    return;
  }

  sensors.requestTemperatures();

  float temperatureC = sensors.getTempCByIndex(0);

  if (temperatureC == DEVICE_DISCONNECTED_C) {
    failedPostCount++;
    Serial.println("Telemetry attempt failed: DS18B20 did not return a valid temperature.");
    printRunCounters();
    return;
  }

  Serial.print("Temperature captured: ");
  Serial.print(temperatureC);
  Serial.println(" deg C");

  bool accepted = postTemperatureToHostedAzureFunction(temperatureC);

  if (accepted) {
    successfulPostCount++;
    Serial.println("Telemetry attempt result: accepted.");
  } else {
    failedPostCount++;
    Serial.println("Telemetry attempt result: failed or rejected.");
  }

  printRunCounters();
}

void printRunCounters() {
  Serial.print("Successful POST count: ");
  Serial.println(successfulPostCount);

  Serial.print("Failed/skipped POST count: ");
  Serial.println(failedPostCount);

  Serial.print("Milliseconds since board start: ");
  Serial.println(millis());
}

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println();
  Serial.println("Starting HiveWatch sustained telemetry test...");
  Serial.println("Interval: 5 minutes.");

  sensors.begin();

  Serial.print("Number of temperature devices found: ");
  Serial.println(sensors.getDeviceCount());

  connectToWiFi();

  // Send immediately, then continue on interval.
  sendTelemetryAttempt();

  lastTelemetryAttemptMs = millis();
}

void loop() {
  unsigned long nowMs = millis();

  if (nowMs - lastTelemetryAttemptMs >= TELEMETRY_INTERVAL_MS) {
    lastTelemetryAttemptMs = nowMs;
    sendTelemetryAttempt();
  }

  delay(1000);
}


