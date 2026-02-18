# Implementation Plan: BLE Provisioning & Mobile Setup

## Phase 1: ESP32 BLE Service Implementation [checkpoint: 703f622]
- [x] Task: BLE Server Scaffolding dc7bd11
    - [ ] Implement `BleProvisioningService` using `nanoFramework.Device.Bluetooth`.
    - [ ] Define Service and Characteristic UUIDs.
- [x] Task: Credential Handling & Storage 668bff6
    - [ ] Implement logic to receive SSID/Password via GATT Write.
    - [ ] Implement NVS storage logic for Wi-Fi settings.
- [x] Task: Conductor - User Manual Verification 'Phase 1: ESP32 BLE Service Implementation' (Protocol in workflow.md) 703f622

## Phase 2: .NET MAUI BLE Client [checkpoint: d2b52b9]
- [x] Task: Mobile Project Scaffolding d31d5d0
    - [ ] Create .NET MAUI project.
    - [ ] Add BLE library (e.g., `Plugin.BLE`).
- [x] Task: Provisioning UI & Logic 8294987
    - [ ] Implement BLE scanning and device selection.
    - [ ] Create form for Wi-Fi credentials and send to ESP32.
- [x] Task: Conductor - User Manual Verification 'Phase 2: .NET MAUI BLE Client' (Protocol in workflow.md) d2b52b9

## Phase 3: Connectivity Transition
- [ ] Task: Wi-Fi Connection Logic
    - [ ] Implement ESP32 logic to stop BLE and start Wi-Fi after provisioning.
    - [ ] Implement IP address reporting to the mobile app or server.
- [ ] Task: Conductor - User Manual Verification 'Phase 3: Connectivity Transition' (Protocol in workflow.md)
