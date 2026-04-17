# UniversalFeeder — Wiring Instructions

## Components

- ESP32 development board
- BTS7960 high-current DC motor driver module
- JGY-370 12V DC gearmotor
- tatoko active buzzer (continuous beep type, 3–24V)
- 12V DC power supply (≥2A)
- Breadboard jumper wires

---

## Step 1 — Wire the JGY-370 to the BTS7960

⚠️ **Terminal labels vary between BTS7960 boards.** On the HW-039 (IBT-2) variant tested with this project, the screw terminals from left to right are:

**M−  M+  B+  B−**

- **M− / M+** = motor output
- **B+ / B−** = 12V power input

If the motor free-runs immediately with no logic power, the motor and power terminals are swapped — swap them.

| JGY-370 wire | BTS7960 terminal |
|--------------|------------------|
| Motor lead 1 | M+ |
| Motor lead 2 | M− |

> If the auger spins backward, swap the two motor wires.

---

## Step 2 — BTS7960 Logic Wiring to ESP32

For the simplest setup, keep the BTS7960 permanently enabled and let firmware drive only forward and reverse PWM inputs.

| BTS7960 pin | ESP32 pin | Notes |
|-------------|-----------|-------|
| RPWM        | GPIO 25   | Forward drive |
| LPWM        | GPIO 26   | Reverse drive, held LOW in current firmware |
| R_EN        | 5V        | Keep enabled |
| L_EN        | 5V        | Keep enabled |
| VCC         | 5V / VIN  | Logic power for common BTS7960 modules |
| GND         | GND       | Common ground |

> R_EN, L_EN, and VCC can all share the same 5V rail. The ESP32 GPIO pins still output 3.3V which is sufficient for RPWM and LPWM on most BTS7960 boards.

---

## Step 3 — 12V Power Supply to BTS7960

| Supply | BTS7960 terminal |
|--------|------------------|
| 12V +  | B+ |
| 12V -  | B− |

⚠️ **GND must be common** — connect BTS7960 GND, ESP32 GND, and 12V supply negative together.

> On the common 8-pin control header, **R_IS** and **L_IS** are current-sense outputs. Leave them unconnected for the current firmware.

---

## Step 4 — Active Buzzer → ESP32

The tatoko buzzer is an **active** type (it has its own oscillator — just needs power to beep).

| Buzzer pin | ESP32 pin |
|------------|-----------|
| + (longer leg / marked +) | GPIO 27 |
| - (shorter leg) | GND |

> Operating voltage is 3.3V–5V; GPIO 27 drives it directly at 3.3V which is sufficient.

---

## Step 5 — Power-On Sequence

Always follow this order to avoid load spikes while wiring:

1. Connect all wiring with power off
2. Power the ESP32 from USB
3. Power the BTS7960 from the 12V supply
4. Open the app and send a Chime or Feed Now command
5. Watch the serial monitor on COM4 for confirmation:

```text
I (xxxx) BuzzerControl: Playing buzzer for 3000 ms
I (xxxx) MotorControl: Rotating motor for 5000 ms
```

---

## Pin Summary (Quick Reference)

```text
ESP32           BTS7960             JGY-370 / Power
────────────────────────────────────────────────────────
GPIO 25  ────── RPWM
GPIO 26  ────── LPWM
5V       ────── R_EN
5V       ────── L_EN
5V/VIN   ────── VCC
GND      ────── GND  ───────────── 12V supply -
GPIO 27  ────── Buzzer +
GND      ────── Buzzer -
                 B+    ──────────── 12V supply +
                 B-    ──────────── 12V supply -
                 M+    ──────────── Motor lead 1
                 M-    ──────────── Motor lead 2
                 R_IS  ──────────── leave unconnected
                 L_IS  ──────────── leave unconnected
```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| Motor doesn't move | BTS7960 enable pins not tied high | Check R_EN and L_EN wiring |
| Motor hums but auger does not turn | Auger is jammed or supply current is too low | Clear jam and verify 12V supply current capacity |
| Motor runs backward | Motor polarity reversed | Swap the two motor leads |
| Motor runs continuously at power-up | RPWM or LPWM floating | Check GPIO 25 and GPIO 26 wiring |
| Motor runs continuously after connect | Stale MQTT commands queued on broker | Firmware ignores commands for 5s after connect; power-cycle and wait |
| Motor free-runs with no logic power | Motor and power screw terminals swapped | Swap the two pairs of screw-terminal wires |
| Driver gets hot | Sustained stall or undersized supply | Reduce jam load and verify motor/supply sizing |
| No buzzer sound | Wires swapped | Check + and − polarity |
| ESP32 browning out | Grounds not common or USB supply weak | Recheck common ground and power ESP32 from stable USB |
