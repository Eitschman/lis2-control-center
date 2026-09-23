# Reverse Engineering Notes

## Scope

The original **VL System MCC (Multi Control Center) 2.2.2.6** was statically analyzed to understand interoperability with the VL System L.I.S. 2 hardware.

The proprietary executable is **not** part of this repository and must not be committed.

Analyzed executable metadata:

- file: `MCC.exe`
- format: PE32
- architecture: Intel 80386
- subsystem: Windows GUI
- implementation indicators: Delphi / VCL
- size: 2,019,840 bytes
- SHA-256: `aed761aa91502318d08f58e8ba0f2c02e0e32bae2df749ec14d185b11aacd826`

The executable was not executed as part of this reverse-engineering work.

## Methods

Analysis included:

- PE inspection
- Delphi/VCL resource inspection
- string extraction
- binary pattern searches
- x86 disassembly
- cross-reference analysis
- Delphi string-constant analysis
- inspection of loops constructing serial commands
- examination of MCC configuration files
- comparison with historical open-source implementations
- comparison with MediaPortal-era discussions
- comparison with LCDproc LIS/MPlay material

Raw byte sequences were not accepted as protocol commands without contextual evidence.

## Major results

The strongest LIS2 findings are:

- serial configuration: 19200 baud, 8N1
- line 1: `A1 <column> A7 <text>`
- line 2: `A2 <column> A7 <text>`
- fan control: `AE F0 F1 F2 F3 F4`
- custom characters: `AB CC RR DD`
- brightness states strongly associated with `A5 38` through `A5 3B`

The fan command is particularly important: MCC directly uses percentage values 0..100 for four fan channels.

## LIS2 / MPlay separation

Historical code frequently mixes related VL System devices. The project deliberately separates LIS2 evidence from MPlay evidence.

MPlay-oriented commands and behavior are not imported into the LIS2 driver unless independently validated.

This applies especially to:

- 38400 baud assumptions
- two-fan models
- temperature-sensor commands
- IR functionality
- `A4 7D`
- `AC ...`
- `AF`
- `AA AA`
- large `AD` initialization blocks

## Misleading observations

### A9 A9 A9

Initially interesting because of its byte pattern, but contextual analysis associated it with an internal GUI/data object rather than serial communication. It is not considered a LIS2 protocol command.

### AB 00

MCC contains a distinct `AB 00 ...` behavior in addition to the established per-character/per-row programming loop. Its meaning remains unresolved and it is quarantined from production implementation.

## Detection

MCC contains strings such as:

```text
VL-LIS2-P/B-7
Detection  L.I.S 2
```

This proves MCC contains LIS2-specific detection logic, but does not prove that `VL-LIS2-P/B-7` is transmitted by or returned from the hardware.

Automatic identification remains an open hardware-validation task.

## Repository policy

Do not commit:

- `MCC.exe`
- original MCC installers
- proprietary VL System binaries
- proprietary Winamp binaries

The repository may contain independently authored protocol documentation, source code, diagrams, tests and test vectors required for interoperability.
