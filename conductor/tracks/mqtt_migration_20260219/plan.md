# Implementation Plan: MQTT Migration

## Phase 1: Server-Side MQTT Integration [checkpoint: baa8ccd]
- [x] Task: Project Dependency Updates ff6bd96
    - [ ] Add `MQTTnet` NuGet package to `UniversalFeeder.Server`.
- [x] Task: Implement MqttFeederClient 6614eed
    - [ ] Create `MqttFeederClient` implementing `IFeederClient`.
    - [ ] Configure connection to HiveMQ Cloud.
- [x] Task: Conductor - User Manual Verification 'Phase 1: Server-Side MQTT Integration' (Protocol in workflow.md) baa8ccd

## Phase 2: Firmware MQTT Integration [checkpoint: eb2cc5a]
- [x] Task: Firmware Dependency Updates 558050e
    - [ ] Add `nanoFramework.M2Mqtt` and `nanoFramework.System.Net.Security` to project.
- [x] Task: Implement MqttService 477489c
    - [ ] Create `MqttService` to replace `WebServerService`.
    - [ ] Implement connection and subscription logic.
    - [ ] Implement command parsing and execution.
- [x] Task: Conductor - User Manual Verification 'Phase 2: Firmware MQTT Integration' (Protocol in workflow.md) eb2cc5a

## Phase 3: System Refactoring & Registration Update [checkpoint: 98447a1]
- [x] Task: Update Registration Logic 5de83e0
    - [ ] Change registration to focus on unique hardware IDs rather than IPs.
- [x] Task: Final Cleanup 0a0bafa
    - [ ] Remove legacy HTTP code from firmware and server.
- [x] Task: Conductor - User Manual Verification 'Phase 3: System Refactoring & Registration Update' (Protocol in workflow.md) 98447a1
