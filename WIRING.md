# UniversalFeeder — Wiring Instructions

## Components

- ESP32 development board
- KEAcvise A4988 stepper motor driver (clone, red board, 0.1Ω sense resistors)
- StepperOnline 17HS19-2004S1 NEMA 17 stepper motor (bipolar, 2A, 4-wire)
- tatoko active buzzer (continuous beep type, 3–24V)
- 12V DC power supply (≥2A)
- 100µF electrolytic capacitor
- Multimeter (for setting current limit)

---

## Step 1 — Set A4988 Current Limit (DO THIS FIRST, motor unplugged)

The motor is rated 2A but running at 1A is sufficient for a feeder auger and keeps the driver cool.

**Target Vref = 0.8V** (= 1A × 8 × 0.1Ω sense resistors on clone boards)

1. Power the A4988 board only (3.3V to VDD, GND to GND) — no motor, no 12V yet
2. Place multimeter probes: **red on the trimpot centre**, **black on GND**
3. Turn the trimpot slowly until you read **0.8V**
4. Power off before continuing

---

## Step 2 — Bridge RESET and SLEEP pins

Clone A4988 boards do **not** pre-connect RST and SLP internally.  
Without this bridge the motor will not move at all.

- Find the two adjacent pins labelled **RST** and **SLP** on the driver board
- Connect them together with a short wire or solder bridge

---

## Step 3 — Motor Coil Connections (A4988 → Motor)

The 17HS19-2004S1 has a 4-pin connector with the following wire colours:

| Motor wire | A4988 pin | Coil |
|------------|-----------|------|
| Black      | 2B        | Coil B− |
| Green      | 1B        | Coil B+ |
| Blue       | 1A        | Coil A+ |
| Red        | 2A        | Coil A− |

> **Wrong direction?** Swap Black ↔ Green (or Red ↔ Blue). Never swap between coils.

---

## Step 4 — A4988 → ESP32 Connections

| A4988 pin | ESP32 pin | Notes |
|-----------|-----------|-------|
| STEP      | GPIO 14   | Step pulse |
| DIR       | GPIO 12   | Direction |
| EN        | GPIO 13   | Enable (active LOW) |
| VDD       | 3.3V      | Logic power |
| GND       | GND       | Common ground |
| MS1       | —         | Leave unconnected (full-step mode) |
| MS2       | —         | Leave unconnected |
| MS3       | —         | Leave unconnected |

---

## Step 5 — 12V Power Supply → A4988

| Supply    | A4988 pin |
|-----------|-----------|
| 12V +     | VMOT      |
| 12V −     | GND       |

⚠️ **GND must be common** — connect the 12V supply GND to the same GND rail as the ESP32.

⚠️ **Add a 100µF capacitor** between VMOT and GND, as close to the A4988 board as possible, to absorb back-EMF spikes that can destroy the driver.

---

## Step 6 — Active Buzzer → ESP32

The tatoko buzzer is an **active** type (it has its own oscillator — just needs power to beep).

| Buzzer pin | ESP32 pin |
|------------|-----------|
| + (longer leg / marked +) | GPIO 27 |
| − (shorter leg) | GND |

> Operating voltage is 3.3V–5V; GPIO 27 drives it directly at 3.3V which is sufficient.

---

## Step 7 — Power-On Sequence

Always follow this order to avoid damaging the driver:

1. Connect all wiring (everything off)
2. Power on the 12V supply
3. Power on the ESP32 (USB or separate 5V)
4. Open the app → connect to MQTT → send a Chime or Feed Now command
5. Watch the serial monitor on COM4 for confirmation:

```
I (xxxx) BuzzerControl: Playing buzzer for 1000 ms
I (xxxx) MotorControl: Rotating motor for 5000 ms
```

---

## Pin Summary (Quick Reference)

```
ESP32           A4988           Motor
─────────────────────────────────────────────
GPIO 14  ──────  STEP
GPIO 12  ──────  DIR
GPIO 13  ──────  EN
3.3V     ──────  VDD
GND      ──────  GND  ──────── 12V supply −
GPIO 27  ──────  Buzzer +
GND      ──────  Buzzer −
                 VMOT ──────── 12V supply +
                 1A   ──────── Motor Blue
                 2A   ──────── Motor Red
                 1B   ──────── Motor Green
                 2B   ──────── Motor Black
                 RST  ────┐
                 SLP  ────┘  (bridge together)
```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| Motor doesn't move | RST/SLP not bridged | Bridge them |
| Motor vibrates but no rotation | Wrong coil wiring | Swap Black↔Green |
| Motor runs backward | Direction reversed | Swap Red↔Blue or swap Black↔Green |
| Driver gets very hot | Vref too high | Lower Vref to 0.8V |
| No buzzer sound | Wires swapped | Check + and − polarity |
| ESP32 browning out | Sharing 12V rail for ESP power | Use separate USB/5V for ESP32 |
