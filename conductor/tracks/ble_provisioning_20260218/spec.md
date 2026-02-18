# Specification: BLE Provisioning & Mobile Setup

## Overview
This track implements the initial out-of-the-box experience (OOBE). It enables a user to connect to an unconfigured ESP32 via Bluetooth Low Energy (BLE) using a mobile app to provide Wi-Fi credentials.

## Functional Requirements
- **ESP32 Firmware:**
    - Start BLE Server if no Wi-Fi credentials are found in storage.
    - Expose a GATT Service with a characteristic for SSID and Password.
    - Save received credentials to Non-Volatile Storage (NVS).
    - Transition from BLE mode to Wi-Fi mode upon receiving valid credentials.
- **.NET MAUI Mobile App:**
    - Scan for BLE devices with a specific service UUID.
    - Provide a UI for the user to select a feeder and enter Wi-Fi SSID/Password.
    - Write credentials to the ESP32 via BLE.
    - Listen for the ESP32's assigned IP address (via BLE or a registration endpoint).

## Technical Constraints
- BLE Protocol: GATT.
- Storage: nanoFramework NVS or configuration blocks.
- Mobile: .NET MAUI (iOS/Android).
