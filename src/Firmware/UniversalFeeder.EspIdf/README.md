# UniversalFeeder ESP-IDF Firmware

ESP-IDF-based firmware for the Universal Feeder project, replacing the previous nanoFramework implementation.

## Purpose

This firmware implements BLE-based WiFi provisioning and MQTT command handling for automated pet feeding hardware. The rewrite to ESP-IDF C provides better hardware control, reduced memory footprint, and native ESP32 toolchain support.

## Build Requirements

- ESP-IDF v5.0 or later
- ESP32 target (tested on ESP32-WROOM-32)
- Python 3.8+ (for ESP-IDF tools)

## Build Instructions

### Environment Setup

**Linux/macOS:**
```bash
. $IDF_PATH/export.sh
```

**Windows (PowerShell):**
```powershell
. $env:IDF_PATH/export.ps1
```

If ESP-IDF was installed under `C:\Espressif`, set the tools root before exporting from a regular PowerShell session:
```powershell
$env:IDF_TOOLS_PATH = 'C:\Espressif'
. $env:IDF_PATH\export.ps1
```

**Windows (Command Prompt):**
```bat
%IDF_PATH%\export.bat
```

### Build and Flash

Navigate to the firmware directory:
```bash
cd src/Firmware/UniversalFeeder.EspIdf
```

Configure (optional, uses sdkconfig.defaults):
```bash
idf.py menuconfig
```

Build firmware:
```bash
idf.py build
```

Flash to device:
```bash
idf.py -p <PORT> flash monitor
```

## Flash Instructions

Replace `<PORT>` with your device's serial port:
- **Windows:** COM3, COM4, etc.
- **Linux:** /dev/ttyUSB0, /dev/ttyUSB1, etc.
- **macOS:** /dev/cu.usbserial-*

Flash only:
```bash
idf.py -p COM3 flash
```

Monitor logs:
```bash
idf.py -p COM3 monitor
```

## Current Implementation Status

**Phase 2 (Current):** Provisioning slice in progress
- Basic ESP-IDF project structure
- NVS initialization
- NVS-backed Wi-Fi credential storage
- BLE provisioning GATT service with preserved UUIDs
- Boot-time provisioning mode selection
- Wi-Fi station mode connection from stored or provisioned credentials
- IP address persistence and BLE characteristic updates
- Logging framework
- Contract documentation (see docs/contracts.md)

**Planned:**
- MQTT client integration
- Feed motor control
- Chime speaker control
- Local schedule fallback

## Contract Preservation

This firmware preserves the existing BLE and MQTT contracts used by the mobile app and server. See [docs/contracts.md](docs/contracts.md) for detailed protocol documentation.

## Project Structure

```
UniversalFeeder.EspIdf/
├── CMakeLists.txt          # Top-level build configuration
├── sdkconfig.defaults      # Default ESP-IDF settings
├── README.md               # This file
├── docs/
│   └── contracts.md        # BLE/MQTT protocol contracts
└── main/
    ├── CMakeLists.txt      # Main component build config
    ├── app_main.c          # Application entry point
    └── include/
        └── app_config.h    # Configuration constants
```

## Development Notes

- Preserves existing mobile/server contracts where practical
- Uses native ESP-IDF APIs for BLE (NimBLE) and MQTT
- Contract UUIDs and topic patterns locked to maintain backward compatibility
- Future enhancements should validate against docs/contracts.md
