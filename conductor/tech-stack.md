# Tech Stack: Universal Auto-Feeder

## 1. Device Firmware (IoT)
- **Runtime:** [.NET nanoFramework](https://www.nanoframework.net/)
- **Language:** C#
- **Hardware Platform:** ESP32 (WROOM/WROVER)
- **Key Libraries:**
  - `nanoFramework.M2Mqtt` (MQTT Client)
  - `nanoFramework.System.Net.Security` (TLS for HiveMQ Cloud)
  - `nanoFramework.Hardware.Esp32` (PWM for Buzzer, GPIO for A4988)

## 2. Server Layer (Backend & Dashboard)
- **Framework:** Blazor Server (.NET 8/9)
- **Database:** SQLite
- **Automation:** Quartz.NET (Background scheduling)
- **Communication:** [MQTTnet](https://github.com/dotnet/MQTTnet) (for publishing commands to HiveMQ)

## 3. Mobile Application (Setup)
- **Framework:** .NET MAUI
- **Language:** C#
- **Capabilities:**
  - BLE scanning and GATT characteristic writing for Wi-Fi provisioning.
  - REST client for device registration with the Blazor Server.

## 4. Communication Protocols
- **Broker:** HiveMQ Cloud (MQTT)
- **Provisioning:** BLE (Mobile -> ESP32)
- **Command & Control:** MQTT Pub/Sub (Server -> Broker -> ESP32)
- **Telemetry:** MQTT (ESP32 -> Broker -> Server)

## 5. Development & Infrastructure
- **IDE:** Visual Studio 2022
- **Version Control:** Git
- **Testing:** xUnit / Moq (for Server/Mobile), nanoFramework Test Framework (for Firmware)
