# Implementation Plan: ESP-IDF Firmware Rewrite

## Overview
Rewrite the ESP32 firmware from nanoFramework C# to ESP-IDF C while preserving the mobile BLE provisioning flow and the server MQTT contract. The firmware should remain a thin appliance layer so the rest of the project can continue to center on .NET mobile, server, and product work.

## Phase 1: Contract Lock And Project Skeleton [checkpoint: contract-and-skeleton]
- [ ] Task: Lock device-facing contracts
  - [ ] Confirm BLE service UUID and characteristic UUIDs to preserve from the current firmware.
  - [ ] Confirm MQTT topic and payload contract to preserve from the current firmware.
  - [ ] Confirm feeder identity strategy used by server and firmware.
- [ ] Task: Create ESP-IDF firmware project structure
  - [ ] Add a new firmware project directory for ESP-IDF.
  - [ ] Add build configuration, flash targets, and a minimal app entry point.
  - [ ] Add a central config header for pins, broker settings, and feature flags.
- [ ] Acceptance
  - [ ] Firmware builds cleanly under ESP-IDF.
  - [ ] Device boots and emits stable serial logs.
  - [ ] No mobile or server contracts have been changed.

## Phase 2: Provisioning Slice [checkpoint: provisioning-slice]
- [ ] Task: Implement storage and boot mode selection
  - [ ] Use NVS to store and retrieve Wi-Fi credentials.
  - [ ] Decide at boot whether to enter provisioning mode or normal mode.
- [ ] Task: Implement BLE provisioning service
  - [ ] Recreate the current GATT service and characteristics.
  - [ ] Accept SSID and password writes from the mobile app.
  - [ ] Expose or notify assigned IP after successful join.
- [ ] Acceptance
  - [ ] Mobile app can discover the device and provision credentials.
  - [ ] Credentials survive reboot.
  - [ ] Assigned IP can be surfaced to the app using the preserved BLE contract.

## Phase 3: Connectivity Slice [checkpoint: connectivity-slice]
- [ ] Task: Implement Wi-Fi manager
  - [ ] Connect automatically using stored credentials.
  - [ ] Surface connection state and IP address to the application state machine.
- [ ] Task: Implement MQTT client
  - [ ] Connect to HiveMQ Cloud over TLS.
  - [ ] Subscribe to `feeders/{feederId}/commands`.
  - [ ] Implement reconnect with bounded backoff.
- [ ] Acceptance
  - [ ] Device connects to Wi-Fi on boot with stored credentials.
  - [ ] Device connects to HiveMQ Cloud and receives command traffic.
  - [ ] Power cycling the device does not require re-provisioning.

## Phase 4: Hardware Control Slice [checkpoint: hardware-slice]
- [ ] Task: Implement motor controller
  - [ ] Port step, direction, and enable control for the A4988-driven motor.
  - [ ] Preserve the duration-driven motor command behavior used by the current firmware.
- [ ] Task: Implement buzzer driver
  - [ ] Port buzzer control using LEDC/PWM.
  - [ ] Preserve basic volume and duration behavior used by the current firmware.
- [ ] Acceptance
  - [ ] Manual hardware tests confirm motor rotation for target durations.
  - [ ] Manual hardware tests confirm audible buzzer output with expected duration.
  - [ ] No unstable GPIO behavior is observed during repeated runs.

## Phase 5: Feeding And Fallback Slice [checkpoint: feeding-and-fallback]
- [ ] Task: Implement command parser and dispatch
  - [ ] Parse the current MQTT JSON command schema.
  - [ ] Dispatch `feed` and `chime` commands to hardware services.
- [ ] Task: Implement feeding sequence orchestration
  - [ ] Recreate the current chime-plus-feed sequence.
  - [ ] Serialize execution so overlapping commands do not corrupt device state.
- [ ] Task: Implement local fallback schedule
  - [ ] Define the minimum viable offline schedule behavior.
  - [ ] Trigger fallback feedings when cloud connectivity is unavailable long enough to require local operation.
- [ ] Acceptance
  - [ ] Publishing the current server command payloads triggers the expected hardware behavior.
  - [ ] The device can feed on a local fallback schedule when cloud connectivity is lost.
  - [ ] Command handling remains stable across reconnects and repeated command bursts.

## Phase 6: Hardening And Cutover [checkpoint: hardening-and-cutover]
- [ ] Task: End-to-end validation
  - [ ] Validate BLE provisioning from the mobile app.
  - [ ] Validate MQTT command flow from the server.
  - [ ] Validate reboot, reconnect, and power-loss recovery.
- [ ] Task: Operational cleanup
  - [ ] Document build, flash, and log inspection workflow for the ESP-IDF firmware.
  - [ ] Mark the nanoFramework firmware as superseded once the new firmware is accepted.
- [ ] Acceptance
  - [ ] A single documented firmware workflow is sufficient for build, flash, and debug.
  - [ ] The ESP-IDF firmware can replace the nanoFramework firmware for normal project use.

## Module Mapping
- Current `Program.cs` -> `app_main` plus application state machine.
- Current `BleProvisioningService.cs` -> BLE provisioning component using NimBLE.
- Current `WifiConfigurationService.cs` -> NVS-backed Wi-Fi manager.
- Current `MqttService.cs` -> MQTT connectivity and command subscription component.
- Current `MotorService.cs` -> GPIO-based motor controller.
- Current `BuzzerService.cs` -> LEDC-based buzzer driver.
- Current `FeedingSequenceService.cs` -> feeding orchestration component.
- Current shared MQTT constants -> shared contract reference used to preserve server compatibility.

## Validation Strategy
- Unit tests: property-based or compact logic tests only where parsing or state handling benefits from it.
- Integration tests: real hardware, real Wi-Fi, real MQTT broker connectivity wherever practical.
- End-to-end tests: deterministic command sequences that exercise provisioning, reconnect, chime, feed, and fallback behavior.

## Risks
- BLE contract drift could force mobile changes.
  - Mitigation: lock UUIDs and provisioning flow before coding.
- TLS and broker configuration could create slow early feedback loops.
  - Mitigation: validate Wi-Fi and broker connectivity in an isolated slice before adding hardware orchestration.
- Offline fallback can expand scope if it is defined too broadly.
  - Mitigation: implement a narrow, explicit MVP fallback schedule first.
- Hardware timing may differ from the nanoFramework implementation.
  - Mitigation: validate motor and buzzer behavior independently before integrating command orchestration.

## Recommended Execution Order
1. Create the ESP-IDF project skeleton.
2. Lock BLE and MQTT contracts from the current codebase.
3. Implement provisioning and Wi-Fi.
4. Implement MQTT connectivity.
5. Implement motor and buzzer drivers.
6. Implement feeding sequence and fallback schedule.
7. Run end-to-end validation and cut over.
