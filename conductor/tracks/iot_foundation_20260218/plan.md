# Implementation Plan: IoT Foundation

## Phase 1: ESP32 Firmware Development [checkpoint: a10e0b7]
- [x] Task: Project Scaffolding (nanoFramework) cdd4748
    - [ ] Create nanoFramework project for ESP32.
    - [ ] Configure GPIO pins for A4988 (Step/Dir/Enable) and Buzzer (PWM).
- [x] Task: Hardware Driver Implementation 07a2a8b
    - [ ] Write tests for motor step logic.
    - [ ] Implement `MotorService` for rotation.
    - [ ] Implement `BuzzerService` for PWM chime.
- [x] Task: Web Server Endpoints ba59566
    - [ ] Write tests for HTTP request parsing.
    - [ ] Implement `/feed` and `/chime` endpoints.
- [x] Task: Conductor - User Manual Verification 'Phase 1: ESP32 Firmware Development' (Protocol in workflow.md) a10e0b7

## Phase 2: Blazor Server Manual Control
- [x] Task: Server Project Scaffolding 214e1e0
    - [ ] Create Blazor Server project (.NET 8/9).
    - [ ] Set up SQLite with EF Core for basic configuration storage.
- [ ] Task: Manual Trigger Logic
    - [ ] Implement `FeederClient` to send HTTP requests to the ESP32.
    - [ ] Create UI with "Manual Feed" button and IP configuration.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: Blazor Server Manual Control' (Protocol in workflow.md)
