# Initial Concept
PROJECT: UNIVERSAL AUTO-FEEDER (DOTNET/IOT)
1. DEVICE FIRMWARE (ESP32 / .NET nanoFramework)
2. SERVER LAYER (Laptop / Blazor Server)
3. MOBILE APP (.NET MAUI)
4. CALIBRATION & TRAINING LOGIC

# Product Definition: Universal Auto-Feeder

## Mission & Vision
To provide a versatile, IoT-driven feeding solution that simplifies animal care for both hobby farmers and pet owners. By combining universal hardware design with precise software calibration and behavioral training cues, the system ensures animals are fed consistently and safely.

## Target Audience
- **Small-Scale Hobby Farmers:** Owners of sheep, pigs, or other livestock needing automated, reliable feeding schedules.
- **Residential Pet Owners:** Owners of dogs or cats looking for a high-tech, customizable feeding solution.

## Core Goals
- **Automated Consistency:** Centralized management of feeding schedules to ensure timely delivery of nutrition.
- **Behavioral Training:** Use of an integrated chime ("Sound-First" sequence) to condition animals for feeding.
- **Universal Versatility:** A single physical hardware design capable of handling various feed types through software-based "Grams Per Second" calibration.

## MVP Features (First Release)
- **Functional Firmware:** ESP32-based control of Nema 17 motors and buzzers via HTTP endpoints.
- **Web-Based Management:** Blazor Server interface for creating, editing, and deleting feeding schedules.
- **Seamless Setup:** .NET MAUI mobile application for BLE-based Wi-Fi provisioning of devices.
- **Manual Control:** On-demand manual feeding triggers via the web dashboard.

## System Reliability & Error Handling
- **Robust Communication:** Failed HTTP calls to feeders will retry 3 times with logging and immediate user alerts.
- **Local Fallback:** In the event of prolonged disconnection, feeders will utilize a local fallback schedule to ensure animals are not missed.
