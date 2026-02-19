# Implementation Plan: MQTT Migration

## Phase 1: Server-Side MQTT Integration [checkpoint: baa8ccd]
- [x] Task: Project Dependency Updates ff6bd96
    - [ ] Add `MQTTnet` NuGet package to `UniversalFeeder.Server`.
- [x] Task: Implement MqttFeederClient 6614eed
    - [ ] Create `MqttFeederClient` implementing `IFeederClient`.
    - [ ] Configure connection to HiveMQ Cloud.
- [x] Task: Conductor - User Manual Verification 'Phase 1: Server-Side MQTT Integration' (Protocol in workflow.md) baa8ccd

## Phase 2: Firmware MQTT Integration
- [ ] Task: Firmware Dependency Updates
    - [ ] Add `nanoFramework.M2Mqtt` and `nanoFramework.System.Net.Security` to project.
- [ ] Task: Implement MqttService
    - [ ] Create `MqttService` to replace `WebServerService`.
    - [ ] Implement connection and subscription logic.
    - [ ] Implement command parsing and execution.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: Firmware MQTT Integration' (Protocol in workflow.md)

## Phase 3: System Refactoring & Registration Update
- [ ] Task: Update Registration Logic
    - [ ] Change registration to focus on unique hardware IDs rather than IPs.
- [ ] Task: Final Cleanup
    - [ ] Remove legacy HTTP code from firmware and server.
- [ ] Task: Conductor - User Manual Verification 'Phase 3: System Refactoring & Registration Update' (Protocol in workflow.md)
