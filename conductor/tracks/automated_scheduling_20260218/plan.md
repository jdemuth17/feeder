# Implementation Plan: Automated Scheduling

## Phase 1: Data Model & UI
- [x] Task: Create Schedule Model 2c860c9
    - [ ] Add `FeedingSchedule` model (Id, FeederId, TimeOfDay, AmountInGrams, IsEnabled).
    - [ ] Update `FeederContext`.
- [x] Task: Migration & UI d16942a
    - [ ] Create and apply EF Core migration.
    - [ ] Create `Schedules.razor` page for CRUD operations on schedules.
- [~] Task: Conductor - User Manual Verification 'Phase 1: Data Model & UI' (Protocol in workflow.md)

## Phase 2: Quartz.NET Integration
- [x] Task: Implement Feeding Job 4c48f8e
    - [ ] Create `FeedingJob` implementing `IJob`.
    - [ ] Implement logic to find due schedules and trigger `FeederClient`.
- [x] Task: Background Worker Configuration 94051fd
    - [ ] Configure Quartz in `Program.cs` to run the check every minute.
- [~] Task: Conductor - User Manual Verification 'Phase 2: Quartz.NET Integration' (Protocol in workflow.md)

## Phase 3: Feeder Registration API
- [ ] Task: Create Registration Endpoint
    - [ ] Implement Minimal API endpoint in `Program.cs` to POST new feeders.
- [ ] Task: Update Mobile App Registration
    - [ ] Update `ProvisioningViewModel` to call the Server API after successful BLE provisioning.
- [ ] Task: Conductor - User Manual Verification 'Phase 3: Feeder Registration API' (Protocol in workflow.md)

