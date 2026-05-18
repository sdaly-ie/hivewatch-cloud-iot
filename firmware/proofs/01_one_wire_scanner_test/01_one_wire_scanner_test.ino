#include <OneWire.h>

#define ONE_WIRE_BUS 4  // ESP32 D4 

OneWire oneWire(ONE_WIRE_BUS);

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println();
  Serial.println("Starting 1-Wire device scan on D4...");

  byte address[8];
  bool deviceFound = false;

  oneWire.reset_search();

  while (oneWire.search(address)) {
    deviceFound = true;

    Serial.print("Found device with ROM code: ");

    for (byte i = 0; i < 8; i++) {
      if (address[i] < 16) {
        Serial.print("0");
      }

      Serial.print(address[i], HEX);

      if (i < 7) {
        Serial.print(" ");
      }
    }

    Serial.println();

    if (OneWire::crc8(address, 7) != address[7]) {
      Serial.println("CRC check failed.");
    } 
    else if (address[0] == 0x28) {
      Serial.println("This is a DS18B20.");
    } 
    else {
      Serial.println("1-Wire device found, but it is not identified as a DS18B20.");
    }

    Serial.println();
  }

  if (!deviceFound) {
    Serial.println("No 1-Wire devices found.");
  }

  oneWire.reset_search();
}

void loop() {
  // No repeated loop needed for this test.
}
