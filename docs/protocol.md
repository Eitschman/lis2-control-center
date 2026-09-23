# VL System L.I.S. 2 Protocol Notes

This document records the current protocol knowledge for the target **VL System L.I.S. 2** hardware.

Protocol facts are assigned confidence levels:

- **CONFIRMED** — validated on real LIS2 hardware.
- **MCC-CONFIRMED** — clearly established by reverse engineering the original MCC implementation.
- **SUPPORTED** — supported by strong MCC and historical evidence.
- **EXPERIMENTAL** — plausible but awaiting hardware validation.
- **UNKNOWN** — insufficient evidence.

The goal of the first hardware-testing phase is to promote MCC-CONFIRMED and SUPPORTED behavior to CONFIRMED.

## LIS2 versus MPlay

Historical software and discussions sometimes conflate VL System LIS2 and MPlay devices. They must be treated as different targets.

LIS2 target characteristics:

- 20x2 VFD
- 19200 baud
- four fan channels
- fan command family `AE F0 ...`
- programmable custom characters

MPlay material mentioning 38400 baud, two fans, temperature sensors, IR or commands such as `A4 7D`, `AC ...`, `AF` and `AA AA` must not automatically be applied to LIS2.

## Serial configuration

**Confidence: MCC-CONFIRMED**

- 19200 baud
- 8 data bits
- no parity
- 1 stop bit
- no handshake
- DTR and RTS initially disabled unless hardware testing proves otherwise

## Display

### Clear / reset

```text
A0
```

**Confidence: SUPPORTED**

The exact distinction between display clear and broader device/display-state reset still needs hardware validation.

### Line 1

```text
A1 <column> A7 <text>
```

**Confidence: MCC-CONFIRMED**

Example, write `HELLO` at column 0:

```text
A1 00 A7 48 45 4C 4C 4F
```

### Line 2

```text
A2 <column> A7 <text>
```

**Confidence: MCC-CONFIRMED**

Logical columns are currently assumed to be 0..19.

MCC often constructs a complete line as:

```text
A1 00 A7 <20 characters> 00
A2 00 A7 <20 characters> 00
```

Whether the trailing `00` is required, ignored, or merely an MCC implementation artifact is UNKNOWN.

Initial text handling should use safe ASCII. The actual VFD character ROM/code page remains to be determined.

## Brightness

**Confidence: SUPPORTED**

| Level | Command |
|---|---|
| 100% | `A5 38` |
| 75% | `A5 39` |
| 50% | `A5 3A` |
| 25% | `A5 3B` |

MCC exposes an OFF state in some UI/configuration contexts, but the corresponding protocol behavior is currently UNKNOWN. Production code should initially expose only verified/tested states.

## Fan control

**Confidence: MCC-CONFIRMED**

```text
AE F0 F1 F2 F3 F4
```

`F1` through `F4` are direct percentage values from decimal 0 through 100, encoded as bytes `00` through `64`.

Example:

```text
AE F0 32 32 4B 64
```

means:

- Fan 1: 50%
- Fan 2: 50%
- Fan 3: 75%
- Fan 4: 100%

The older uncertain LCDproc sequence `00 AE 00 00 F1 00 F2 00 F3 00 F4 00 00` is not the target implementation.

### Safety requirements

Fan control must be safety-oriented and independent from display/UI/network integrations.

Recommended initial defaults:

- minimum: 30%
- maximum: 100%
- fail-safe: 100%
- missing/invalid sensor: 100%
- controller error: 100%
- fan stop permission: false

Values must be clamped to 0..100.

## Custom characters

**Confidence: MCC-CONFIRMED**

The device supports eight programmable characters. MCC behavior strongly establishes:

```text
AB <character> <row> <pixel-data>
```

or:

```text
AB CC RR DD
```

where:

- `CC`: character slot 1..8
- `RR`: row 0..7
- `DD`: five-bit row bitmap

Example slot 1:

```text
AB 01 00 00
AB 01 01 04
AB 01 02 0E
AB 01 03 1F
AB 01 04 0E
AB 01 05 04
AB 01 06 00
AB 01 07 00
```

A separate `AB 00 ...` form exists in MCC but its meaning is UNKNOWN and must not be used in production until understood.

## Rejected / quarantined observations

### A9 A9 A9

Investigation associates this sequence with an internal GUI/data object rather than LIS2 serial protocol. It is rejected as a protocol command.

### AA AA

Primarily associated with historical MPlay/LIS-family IR/display initialization. Do not transmit automatically to LIS2.

### AD <large block>

Historical MPlay material uses this for bulk character/display initialization. It is excluded from the initial LIS2 implementation.

## Device detection

MCC contains strings including `VL-LIS2-P/B-7` and `Detection  L.I.S 2`, but it has not been established that the device itself returns this identifier.

Initial implementation should:

1. enumerate COM ports;
2. let the user select a port;
3. store the selection;
4. reconnect to that port.

Automatic identification should only be added after a safe handshake has been established.

## Initial confidence table

| Operation | Confidence |
|---|---|
| Serial 19200 8N1 | MCC-CONFIRMED |
| Line 1 `A1 pos A7 text` | MCC-CONFIRMED |
| Line 2 `A2 pos A7 text` | MCC-CONFIRMED |
| Fans `AE F0 F1 F2 F3 F4` | MCC-CONFIRMED |
| Custom chars `AB CC RR DD` | MCC-CONFIRMED |
| `A0` | SUPPORTED |
| Brightness 100/75/50/25 | SUPPORTED |
| Brightness OFF | UNKNOWN |
| Automatic device detection | UNKNOWN |
| `AB 00 ...` | UNKNOWN |
| `A9 A9 A9` | REJECTED / not protocol |

## Hardware validation order

1. Open COM port at 19200 8N1 and send nothing.
2. Send `A0` and observe behavior.
3. Send `A1 00 A7 48 45 4C 4C 4F`; expect `HELLO` on line 1.
4. Test line 2.
5. Test supported brightness commands.
6. Program one custom character.
7. Test fans, starting at 100% and making modest reductions.

Do not initially test 0% fan output on cooling-critical channels.

## Open protocol questions

- Is the full-line trailing zero required?
- What exactly does `A0` reset?
- How is brightness OFF encoded?
- What is `AB 00 ...`?
- Does the device return any response?
- Is there a safe identification handshake?
- What is the exact character ROM/code page?
- What happens for columns above 19?
- Can firmware version information be read?
- Do low fan values have special semantics?
