# Implementation Plan: System Configuration

## Phase 1: Settings Data Model [checkpoint: 9b58f57]
- [x] Task: Create SystemSetting Model a66e668
    - [ ] Add `SystemSetting` model (Key, Value).
    - [ ] Update `FeederContext`.
- [x] Task: Migration & Repository f43bc66
    - [ ] Create and apply EF Core migration.
    - [ ] Create `SettingsService` for centralized access to global config.
- [x] Task: Conductor - User Manual Verification 'Phase 1: Settings Data Model' (Protocol in workflow.md) 9b58f57

## Phase 2: Settings UI [checkpoint: 8a0484c]
- [x] Task: Create Settings Page 8a0484c
    - [ ] Build `Settings.razor` with fields for MQTT Host, User, and Password.
    - [ ] Implement save logic.
- [x] Task: Navigation 8a0484c
    - [ ] Add "Settings" link to the sidebar.
- [x] Task: Conductor - User Manual Verification 'Phase 2: Settings UI' (Protocol in workflow.md) 8a0484c

## Phase 3: Runtime Integration [checkpoint: 5c9893c]
- [x] Task: Refactor MqttFeederClient 0da2695
    - [ ] Update client to use `SettingsService` for broker connection details.
- [x] Task: Conductor - User Manual Verification 'Phase 3: Runtime Integration' (Protocol in workflow.md) 5c9893c
