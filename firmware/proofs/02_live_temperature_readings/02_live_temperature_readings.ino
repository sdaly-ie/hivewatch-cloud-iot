#include <OneWire.h>
#include <DallasTemperature.h>

#define ONE_WIRE_BUS 4  // ESP32 D4 

OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature sensors(&oneWire);

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println();
  Serial.println("Starting DS18B20 temperature reading test...");

  sensors.begin();

  Serial.print("Number of temperature devices found: ");
  Serial.println(sensors.getDeviceCount());
}

void loop() {
  sensors.requestTemperatures();

  float temperatureC = sensors.getTempCByIndex(0);

  if (temperatureC == DEVICE_DISCONNECTED_C) {
    Serial.println("Error: DS18B20 probe not returning a valid temperature.");
  } else {
    Serial.print("Temperature: ");
    Serial.print(temperatureC);
    Serial.println(" °C");
  }

  delay(2000);
}