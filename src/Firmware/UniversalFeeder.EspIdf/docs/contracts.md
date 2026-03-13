# UniversalFeeder Protocol Contracts

This document defines the BLE and MQTT contracts that the firmware must implement to maintain compatibility with the existing mobile app and server infrastructure.

## Contract Stability

These contracts are **locked** for backward compatibility. Changes require coordination with mobile app and server teams.

Last verified: 2026-03-11  
Source: Existing nanoFramework implementation

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

### Provisioning Flow

1. Device boots and advertises as "Feeder-Setup"
2. Mobile app connects and discovers service/characteristics
3. Mobile app writes SSID to SSID characteristic
4. Mobile app writes password to Password characteristic
5. Firmware attempts WiFi connection
6. On success, firmware writes IP address to IP characteristic and sends notification
7. Mobile app reads IP address
8. BLE connection terminates
9. Device operates in WiFi station mode

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

**Broker:** Configurable via provisioning or NVS storage  
**QoS:** 1 (at least once delivery)  
**Retained:** No  
**Clean Session:** Yes

### Error Handling

- Invalid JSON: Log and ignore
- Unknown action: Log and ignore
- Missing required fields: Use defaults or ignore
- Out-of-range values: Clamp to valid range

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
- Device ID / Feeder ID
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
- [ ] All three characteristic UUIDs are correct
- [ ] SSID/Password write callbacks function correctly
- [ ] IP address read/notify functions correctly
- [ ] MQTT topic pattern matches `feeders/{feederId}/commands`
- [ ] Feed command accepted with correct JSON structure
- [ ] Chime command accepted with correct JSON structure
- [ ] Default values applied when fields missing
- [ ] Mobile app can provision device end-to-end
- [ ] Server can send commands successfully

---

## Change History

- 2026-03-11: Initial contract lock for ESP-IDF rewrite (Phase 1)
