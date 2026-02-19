# Implementation Plan: MQTT Migration

## Phase 1: Server-Side MQTT Integration
- [ ] Task: Project Dependency Updates
    - [ ] Add `MQTTnet` NuGet package to `UniversalFeeder.Server`.
- [ ] Task: Implement MqttFeederClient
    - [ ] Create `MqttFeederClient` implementing `IFeederClient`.
    - [ ] Configure connection to HiveMQ Cloud.
- [ ] Task: Conductor - User Manual Verification 'Phase 1: Server-Side MQTT Integration' (Protocol in workflow.md)

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
