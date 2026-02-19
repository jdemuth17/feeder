# Specification: System Configuration & Secret Management

## Overview
This track provides a centralized interface within the Blazor dashboard to manage global system settings, specifically MQTT broker connection details (Host, Username, Password). This removes the need to restart the server or edit configuration files to change cloud services.

## Functional Requirements
- **Global Settings Storage:**
    - Implement a `SystemSetting` model to store key-value pairs in the SQLite database.
    - Specifically handle `MqttHost`, `MqttUsername`, and `MqttPassword`.
- **Configuration Dashboard:**
    - Create a "Settings" page in the Blazor dashboard.
    - Provide a secure form to update MQTT credentials.
    - Mask the password field by default.
- **Dynamic Client Update:**
    - Update `MqttFeederClient` to retrieve credentials from the database at runtime rather than relying solely on `appsettings.json`.

## Technical Constraints
- Storage: SQLite (EF Core).
- Security: Settings are stored locally in the project's SQLite file.
