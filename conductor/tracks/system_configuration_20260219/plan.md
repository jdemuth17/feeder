# Implementation Plan: System Configuration

## Phase 1: Settings Data Model
- [ ] Task: Create SystemSetting Model
    - [ ] Add `SystemSetting` model (Key, Value).
    - [ ] Update `FeederContext`.
- [ ] Task: Migration & Repository
    - [ ] Create and apply EF Core migration.
    - [ ] Create `SettingsService` for centralized access to global config.
- [ ] Task: Conductor - User Manual Verification 'Phase 1: Settings Data Model' (Protocol in workflow.md)

## Phase 2: Settings UI
- [ ] Task: Create Settings Page
    - [ ] Build `Settings.razor` with fields for MQTT Host, User, and Password.
    - [ ] Implement save logic.
- [ ] Task: Navigation
    - [ ] Add "Settings" link to the sidebar.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: Settings UI' (Protocol in workflow.md)

## Phase 3: Runtime Integration
- [ ] Task: Refactor MqttFeederClient
    - [ ] Update client to use `SettingsService` for broker connection details.
- [ ] Task: Conductor - User Manual Verification 'Phase 3: Runtime Integration' (Protocol in workflow.md)
