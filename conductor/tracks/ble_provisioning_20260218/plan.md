# Implementation Plan: BLE Provisioning & Mobile Setup

## Phase 1: ESP32 BLE Service Implementation
- [ ] Task: BLE Server Scaffolding
    - [ ] Implement `BleProvisioningService` using `nanoFramework.Device.Bluetooth`.
    - [ ] Define Service and Characteristic UUIDs.
- [ ] Task: Credential Handling & Storage
    - [ ] Implement logic to receive SSID/Password via GATT Write.
    - [ ] Implement NVS storage logic for Wi-Fi settings.
- [ ] Task: Conductor - User Manual Verification 'Phase 1: ESP32 BLE Service Implementation' (Protocol in workflow.md)

## Phase 2: .NET MAUI BLE Client
- [ ] Task: Mobile Project Scaffolding
    - [ ] Create .NET MAUI project.
    - [ ] Add BLE library (e.g., `Plugin.BLE`).
- [ ] Task: Provisioning UI & Logic
    - [ ] Implement BLE scanning and device selection.
    - [ ] Create form for Wi-Fi credentials and send to ESP32.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: .NET MAUI BLE Client' (Protocol in workflow.md)

## Phase 3: Connectivity Transition
- [ ] Task: Wi-Fi Connection Logic
    - [ ] Implement ESP32 logic to stop BLE and start Wi-Fi after provisioning.
    - [ ] Implement IP address reporting to the mobile app or server.
- [ ] Task: Conductor - User Manual Verification 'Phase 3: Connectivity Transition' (Protocol in workflow.md)
