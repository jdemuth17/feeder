# Tech Stack: Universal Auto-Feeder

## 1. Device Firmware (IoT)
- **Runtime:** [.NET nanoFramework](https://www.nanoframework.net/)
- **Language:** C#
- **Hardware Platform:** ESP32 (WROOM/WROVER)
- **Key Libraries:**
  - `nanoFramework.Networking.Sntp` (Time sync)
  - `nanoFramework.Device.Bluetooth` (BLE provisioning)
  - `nanoFramework.Hardware.Esp32` (PWM for Buzzer, GPIO for A4988)
  - `nanoFramework.WebServer` (HTTP Endpoints)

## 2. Server Layer (Backend & Dashboard)
- **Framework:** Blazor Server (.NET 8/9)
- **Database:** SQLite
- **Automation:** Quartz.NET (Background scheduling)
- **ORM:** Entity Framework Core (EF Core)
- **Communication:** `HttpClient` (for triggering ESP32 actions)

## 3. Mobile Application (Setup)
- **Framework:** .NET MAUI
- **Language:** C#
- **Capabilities:**
  - BLE scanning and GATT characteristic writing for Wi-Fi provisioning.
  - REST client for device registration with the Blazor Server.

## 4. Communication Protocols
- **Provisioning:** BLE (Mobile -> ESP32)
- **Command & Control:** HTTP/REST (Server -> ESP32)
- **Telemetry:** MQTT (ESP32 -> Server/Broker for status/logs)

## 5. Development & Infrastructure
- **IDE:** Visual Studio 2022
- **Version Control:** Git
- **Testing:** xUnit / Moq (for Server/Mobile), nanoFramework Test Framework (for Firmware)
