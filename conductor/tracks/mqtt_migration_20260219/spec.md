# Specification: MQTT Migration & HiveMQ Integration

## Overview
This track transitions the project's communication architecture from direct local HTTP calls to a robust, cloud-based MQTT pub/sub model using HiveMQ Cloud. This enables reliable remote control without static IPs or complex port forwarding.

## Functional Requirements
- **Firmware (ESP32):**
    - Replace `WebServerService` with an `MqttService`.
    - Connect to HiveMQ Cloud using TLS.
    - Subscribe to `feeders/{feederId}/commands`.
    - Process JSON commands: `{"action": "feed", "ms": 5000}`.
- **Server (Blazor):**
    - Replace `FeederClient` (HTTP) with `MqttFeederClient` using `MQTTnet`.
    - Publish feeding and chime commands to the HiveMQ broker.
    - Update registration to use a unique `FeederId` (likely MAC address or Serial) instead of relying on IP.
- **Security:**
    - Securely store MQTT credentials (Username/Password) provided by HiveMQ Cloud.

## Technical Constraints
- Broker: HiveMQ Cloud (Standard MQTT).
- Firmware Library: `nanoFramework.M2Mqtt`.
- Server Library: `MQTTnet`.
- Authentication: Simple Username/Password + TLS.
