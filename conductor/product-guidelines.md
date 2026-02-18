# Product Guidelines: Universal Auto-Feeder

## Voice and Tone
- **Technical & Precise:** All system communications, UI labels, and notifications must prioritize data accuracy and hardware status. Avoid fluff; focus on providing high-signal information (e.g., "Feeder #12: SSID Handshake Successful" instead of "Your feeder is now online!").

## Visual Identity & UI Design
- **High-Contrast Utility:** The interface must be highly legible in various lighting conditions, especially outdoor environments where high glare is common. Use high-contrast color palettes and bold status indicators.
- **Minimalist & Clean:** Interfaces should be stripped of non-essential elements to ensure that setup and manual feeding triggers are unmistakable and low-friction.

## Code Quality & Documentation
- **Rigorous Enterprise Standards:** 
    - All public APIs and methods must include comprehensive XML documentation.
    - Logic-heavy components (calibration, scheduling, hardware drivers) must maintain 100% unit test coverage.
    - Code should follow strict SOLID principles to ensure maintainability across the different layers (Firmware, Server, Mobile).

## Error Handling & Logging
- **Technical IDs & Descriptions:** Use unique, searchable error codes (e.g., `ERR_ESP32_WIFI_AUTH_FAIL`) accompanied by detailed technical descriptions to facilitate rapid troubleshooting.
- **Action-Oriented Prompts:** When an error occurs, provide the user with clear, actionable steps to resolve the issue (e.g., "Check A4988 driver power rail; verify motor enable pin logic level").

## Hardware/Software Interaction
- **Safety First:** Software must enforce hardware safety limits (e.g., maximum motor run time to prevent overheating) regardless of user input or schedule settings.
- **Real-Time Transparency:** The system should provide the most granular status feedback possible during hardware operations.
