# Specification: IoT Foundation (MVP Core)

## Overview
This track establishes the primary communication link between the Blazor Server and the ESP32 hardware. It focuses on the "Sound-First" feeding sequence and manual control.

## Functional Requirements
- **ESP32 (.NET nanoFramework):**
    - HTTP GET `/feed?ms=X` rotates the Nema 17 motor for X milliseconds.
    - HTTP GET `/chime?vol=X` plays the buzzer via PWM.
    - Implement sequence logic: Chime -> 3s Delay -> Motor Spin.
- **Blazor Server:**
    - Simple UI with a "Feed Now" button.
    - Ability to configure and save the target ESP32 IP address.
    - Basic logging of successful/failed manual triggers.

## Technical Constraints
- Hardware: ESP32 with A4988 driver.
- Protocol: HTTP (REST).
- Code Standards: Rigorous XML documentation and unit tests for control logic.
