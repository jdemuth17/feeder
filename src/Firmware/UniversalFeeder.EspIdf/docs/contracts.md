# UniversalFeeder Protocol Contracts

This document defines the BLE and MQTT contracts that the firmware must implement to maintain compatibility with the existing mobile app and server infrastructure.

## Contract Stability

These contracts are **locked** for backward compatibility. Changes require coordination with mobile app and server teams.

Last verified: 2026-03-20  
Source: Existing nanoFramework implementation and ESP-IDF port

---

## BLE Provisioning Contract

The firmware exposes a BLE GATT service for WiFi provisioning during initial setup.

### Device Discovery

**Device Name Pattern:**
```
Feeder-Setup
```

The device advertises with this name prefix. Mobile apps scan for devices matching this pattern.

### GATT Service

**Service UUID:**
```
4fafc201-1fb5-459e-8fcc-c5c9c331914b
```

### GATT Characteristics

#### SSID Characteristic

**UUID:**
```
beb5483e-36e1-4688-b7f5-ea07361b26a8
```

**Properties:** Write  
**Format:** UTF-8 string  
**Purpose:** Mobile app writes WiFi SSID to this characteristic

**Example:**
```
HomeNetwork
```

#### Password Characteristic

**UUID:**
```
d6e98ba1-8ef4-4594-ba04-0390ea000001
```

**Properties:** Write  
**Format:** UTF-8 string  
**Purpose:** Mobile app writes WiFi password to this characteristic

**Example:**
```
MySecurePassword123
```

#### IP Address Characteristic

**UUID:**
```
e2a00001-8ef4-4594-ba04-0390ea000001
```

**Properties:** Read, Notify  
**Format:** UTF-8 string (IPv4 dotted-decimal)  
**Purpose:** Firmware writes assigned IP address after successful WiFi connection

**Example:**
```
192.168.1.42
```

#### Device ID Characteristic

**UUID:**
```
f4b00001-8ef4-4594-ba04-0390ea000001
```

**Properties:** Read  
**Format:** UTF-8 string (12-char uppercase device identifier)  
**Purpose:** Firmware exposes the unique feeder identifier used for MQTT topic selection and mobile registration flows

**Example:**
```
AABBCCDDEEFF
```

### Provisioning Flow

1. Device boots and advertises as "Feeder-Setup"
2. Mobile app connects and discovers service/characteristics
3. Mobile app writes SSID to SSID characteristic
4. Mobile app writes password to Password characteristic
5. Firmware attempts WiFi connection
6. On success, firmware writes IP address to IP characteristic and sends notification
7. Mobile app reads IP address
8. Mobile app can read the device ID characteristic
9. BLE connection terminates
10. Device operates in WiFi station mode

---

## MQTT Command Contract

After provisioning, the device connects to an MQTT broker and subscribes to a device-specific command topic.

### Topic Pattern

**Subscribe Topic:**
```
feeders/{feederId}/commands
```

Where `{feederId}` is a unique device identifier (typically MAC address or configured ID).

**Example:**
```
feeders/aabbccddeeff/commands
```

### Command Payloads

Commands are sent as JSON payloads.

#### Feed Command

Activates the feed motor for a specified duration.

**Payload:**
```json
{
  "action": "feed",
  "ms": 5000
}
```

**Fields:**
- `action` (string): Must be "feed"
- `ms` (integer): Duration in milliseconds (default: 5000)

**Firmware Behavior:**
- Activate feed motor
- Run for specified duration
- Stop motor
- Optionally publish status/acknowledgment

#### Chime Command

Plays audio through the speaker at a specified volume.

**Payload:**
```json
{
  "action": "chime",
  "vol": 1.0
}
```

**Fields:**
- `action` (string): Must be "chime"
- `vol` (float): Volume level 0.0 to 1.0 (default: 1.0)

**Firmware Behavior:**
- Play chime sound
- Use specified volume
- Optionally publish status/acknowledgment

### MQTT Connection Parameters

**Broker:** Current ESP-IDF build uses the configured HiveMQ Cloud broker URI and credentials compiled into firmware  
**QoS:** 1 (at least once delivery)  
**Retained:** No  
**Clean Session:** Yes

### Error Handling

- Invalid JSON: Log and ignore
- Unknown action: Log and ignore
- Missing required fields: Use defaults or ignore
- Out-of-range values: Clamp to valid range

### Offline Fallback Behavior

Current MVP behavior is intentionally narrow and device-local:
- Fallback only arms when MQTT has been disconnected for 12 hours.
- Once armed, the device issues one local feed using the default duration every 24 hours until MQTT reconnects.
- Any successful `feed` command resets the fallback timer.
- Reconnecting to MQTT immediately suspends fallback feeding.

---

## Implementation Notes

### BLE Implementation (ESP-IDF)

Use NimBLE stack (recommended for ESP-IDF):
- `esp_nimble_hci_init()`
- `nimble_port_init()`
- Register GATT service with specified UUIDs
- Handle characteristic write callbacks
- WiFi connection logic triggers on password write

### MQTT Implementation (ESP-IDF)

Use `esp_mqtt_client`:
- `esp_mqtt_client_init()`
- `esp_mqtt_client_start()`
- Subscribe to `feeders/{feederId}/commands`
- Parse JSON using `cJSON` library
- Dispatch commands to motor/speaker drivers

### Configuration Storage

Use NVS (Non-Volatile Storage) for:
- WiFi credentials (if persistent provisioning desired)
- MQTT broker URL
- Last known IP address
- Calibration parameters

### Security Considerations

- WiFi password transmitted over BLE is vulnerable to sniffing
- Consider TLS for MQTT if broker supports it
- Future: Add BLE pairing/bonding for provisioning security

---

## Validation Checklist

Before releasing firmware changes affecting these contracts:

- [ ] BLE device name matches pattern
- [ ] BLE service UUID is correct
- [ ] All four characteristic UUIDs are correct
- [ ] SSID/Password write callbacks function correctly
- [ ] IP address read/notify functions correctly
- [ ] Device ID read function correctly
- [ ] MQTT topic pattern matches `feeders/{feederId}/commands`
- [ ] Feed command accepted with correct JSON structure
- [ ] Chime command accepted with correct JSON structure
- [ ] Fallback feed triggers after the configured offline interval
- [ ] Default values applied when fields missing
- [ ] Mobile app can provision device end-to-end
- [ ] Server can send commands successfully

---

## Change History

- 2026-03-11: Initial contract lock for ESP-IDF rewrite (Phase 1)
- 2026-03-20: ESP-IDF implementation updated to expose the device ID characteristic and execute MQTT `feed`/`chime` commands through native buzzer, motor, and feeding-sequence modules
- 2026-03-20: Added MVP offline fallback scheduling and a repo-local ESP-IDF build script/task for Windows development
