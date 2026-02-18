# Implementation Plan: IoT Foundation

## Phase 1: ESP32 Firmware Development
- [ ] Task: Project Scaffolding (nanoFramework)
    - [ ] Create nanoFramework project for ESP32.
    - [ ] Configure GPIO pins for A4988 (Step/Dir/Enable) and Buzzer (PWM).
- [ ] Task: Hardware Driver Implementation
    - [ ] Write tests for motor step logic.
    - [ ] Implement `MotorService` for rotation.
    - [ ] Implement `BuzzerService` for PWM chime.
- [ ] Task: Web Server Endpoints
    - [ ] Write tests for HTTP request parsing.
    - [ ] Implement `/feed` and `/chime` endpoints.
- [ ] Task: Conductor - User Manual Verification 'Phase 1: ESP32 Firmware Development' (Protocol in workflow.md)

## Phase 2: Blazor Server Manual Control
- [ ] Task: Server Project Scaffolding
    - [ ] Create Blazor Server project (.NET 8/9).
    - [ ] Set up SQLite with EF Core for basic configuration storage.
- [ ] Task: Manual Trigger Logic
    - [ ] Implement `FeederClient` to send HTTP requests to the ESP32.
    - [ ] Create UI with "Manual Feed" button and IP configuration.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: Blazor Server Manual Control' (Protocol in workflow.md)
