# Specification: Calibration Wizard

## Overview
This track implements a step-by-step wizard in the Blazor Server dashboard to calibrate the feeder. It converts physical weights (Grams) into motor timing (Milliseconds) by calculating "Grams Per Second" for specific feed types.

## Functional Requirements
- **Feed Type Management:**
    - User can select an existing feed type (e.g., "Sheep Pellets", "Dog Kibble") or create a new one.
    - Store calibration values per feed type in the database.
- **Wizard Flow:**
    1. **Select/Create Feed Type:** Start with the material being measured.
    2. **Run Test Dispense:** Trigger the motor for a fixed duration (e.g., 10 seconds).
    3. **Input Weight:** User weighs the dispensed material and enters the value in grams.
    4. **Calculate & Save:** System calculates `Grams / Seconds` and updates the feeder's calibration record.
- **UI/UX:**
    - High-contrast, minimalist design for easy use in various environments.
    - Clear instructions for each step.

## Technical Constraints
- Platform: Blazor Server.
- Storage: SQLite (via EF Core).
- Hardware Interaction: Uses existing `FeederClient` to trigger test dispense.
