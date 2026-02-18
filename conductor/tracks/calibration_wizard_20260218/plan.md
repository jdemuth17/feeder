# Implementation Plan: Calibration Wizard

## Phase 1: Data Model & Service Updates
- [x] Task: Update Feed Models 0390391
    - [ ] Create `FeedType` model (Id, Name, GramsPerSecond).
    - [ ] Update `Feeder` model to reference a `FeedType`.
- [x] Task: Migration & Repository e2c30b0
    - [ ] Create and apply EF Core migration.
    - [ ] Update `FeederContext` and create `FeedTypeService`.
- [~] Task: Conductor - User Manual Verification 'Phase 1: Data Model & Service Updates' (Protocol in workflow.md)

## Phase 2: Wizard UI Implementation
- [ ] Task: Create Calibration Wizard Component
    - [ ] Step 1: Feed Type Selection/Creation UI.
    - [ ] Step 2: Test Dispense Trigger UI (using `FeederClient`).
    - [ ] Step 3: Weight Input & Calculation Logic.
- [ ] Task: Navigation & Integration
    - [ ] Add Calibration link to sidebar or Feeder details page.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: Wizard UI Implementation' (Protocol in workflow.md)
