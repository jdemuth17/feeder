# Specification: Automated Scheduling

## Overview
This track implements the core automation logic for the Universal Auto-Feeder. It allows users to define recurring feeding schedules via the Blazor Server, which are then executed using Quartz.NET background jobs.

## Functional Requirements
- **Schedule Management:**
    - Create, Edit, and Delete feeding schedules.
    - Fields: Time of Day, Amount (in Grams), Enabled/Disabled toggle.
    - Link schedules to specific feeders.
- **Automation Logic (Quartz.NET):**
    - A background job runs every minute to check for active schedules.
    - When a schedule time matches the current time, calculate the motor duration (ms) based on the feeder's calibration (Grams Per Second).
    - Trigger the `/feed` command on the target ESP32.
- **Logging:**
    - Automatically record success/failure of every scheduled feeding in the `Logs` table.

## Technical Constraints
- Library: Quartz.NET (Extensions.Hosting).
- Duration Calculation: `DurationMs = (DesiredGrams / GramsPerSecond) * 1000`.
- Scheduling Granularity: 1 minute.
