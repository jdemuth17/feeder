# Implementation Plan: Automated Scheduling

## Phase 1: Data Model & UI
- [ ] Task: Create Schedule Model
    - [ ] Add `FeedingSchedule` model (Id, FeederId, TimeOfDay, AmountInGrams, IsEnabled).
    - [ ] Update `FeederContext`.
- [ ] Task: Migration & UI
    - [ ] Create and apply EF Core migration.
    - [ ] Create `Schedules.razor` page for CRUD operations on schedules.
- [ ] Task: Conductor - User Manual Verification 'Phase 1: Data Model & UI' (Protocol in workflow.md)

## Phase 2: Quartz.NET Integration
- [ ] Task: Implement Feeding Job
    - [ ] Create `FeedingJob` implementing `IJob`.
    - [ ] Implement logic to find due schedules and trigger `FeederClient`.
- [ ] Task: Background Worker Configuration
    - [ ] Configure Quartz in `Program.cs` to run the check every minute.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: Quartz.NET Integration' (Protocol in workflow.md)
