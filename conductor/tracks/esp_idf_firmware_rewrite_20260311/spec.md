# Specification: ESP-IDF Firmware Rewrite

## Overview
This track replaces the ESP32 nanoFramework firmware with native ESP-IDF C firmware. The goal is to reduce platform instability and debugging friction while preserving the existing .NET mobile and server integration points.

## Goals
- Keep .NET MAUI mobile provisioning support intact.
- Keep Blazor/MQTT server integration intact.
- Move the ESP32 runtime, device drivers, and device-side orchestration to ESP-IDF C.
- Preserve the current device behavior where possible and allow only small cleanup changes that simplify the firmware.
- Include the local fallback schedule behavior in the first rewrite pass.

## Functional Requirements
- Boot reliably on ESP32-D0WD-V3 hardware without nanoFramework runtime dependencies.
- Start in BLE provisioning mode when Wi-Fi credentials are missing.
- Expose BLE GATT characteristics for SSID, password, and assigned IP address.
- Store Wi-Fi credentials in non-volatile storage.
- Connect to Wi-Fi automatically when credentials are present.
- Connect to HiveMQ Cloud over TLS and subscribe to feeder command topics.
- Process command payloads compatible with the existing MQTT contract.
- Control the stepper motor and buzzer on the current hardware pins.
- Execute the feeding sequence used by the current firmware.
- Maintain a local fallback schedule if cloud connectivity is unavailable for a prolonged period.

## Existing Contract Requirements
### BLE
- Preserve the current BLE service UUID and characteristic UUIDs used by the mobile app unless a specific incompatibility is discovered.
- Preserve the provisioning flow: write SSID, write password, notify or expose assigned IP.

### MQTT
- Preserve the existing topic structure: `feeders/{feederId}/commands`.
- Preserve the existing command schema used by the server:
  - `{"action":"feed","ms":5000}`
  - `{"action":"chime","vol":1.0}`
- Preserve the feeder ID strategy based on device identity.

## Technical Constraints
- Target platform: ESP32 via ESP-IDF C.
- Firmware should use built-in ESP-IDF capabilities where practical:
  - NVS for storage
  - NimBLE for BLE GATT
  - `esp_wifi` for Wi-Fi
  - `esp-mqtt` or equivalent ESP-IDF-friendly MQTT client
  - FreeRTOS tasks/timers/events for coordination
  - LEDC/GPIO for buzzer and motor control
- Avoid introducing Rust, Arduino framework, or a second abstraction layer in the rewrite.

## Non-Goals
- Rewriting the server or mobile applications.
- Changing the product-level user experience unless required by a firmware limitation.
- Adding advanced calibration or sensor feedback beyond what already exists in scope.

## Success Criteria
- The ESP32 boots and logs reliably.
- The mobile app can provision Wi-Fi credentials over BLE.
- The device reconnects to Wi-Fi and MQTT after reboot.
- Existing server commands trigger chime and feed actions successfully.
- The fallback schedule can trigger feeding without cloud connectivity.
- Firmware debugging and flashing are reliable enough to support normal iteration.
