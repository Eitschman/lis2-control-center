# LIS2 Control Center

Modern Windows control software for the **VL System L.I.S. 2** VFD display and four-channel fan controller.

> **Status:** Planning / Reverse Engineering  
> **Target:** Windows 11 x64  
> **Implementation:** C# / .NET 8 or newer  
> **License:** MIT

## Vision

LIS2 Control Center turns the VL System L.I.S. 2 into a modern programmable Windows information display, Winamp/media display, hardware-monitoring frontend and safe four-channel fan controller.

The project replaces the obsolete MCC software with an independent implementation based on interoperability research and real-hardware validation.

## Target hardware

- VL System L.I.S. 2
- 20x2 character VFD
- four independently controllable fan channels
- eight programmable custom characters
- serial communication at 19200 baud, 8N1

## Planned features

- native LIS2 serial driver
- flexible 20x2 display renderer
- page rotation, scrolling and event overlays
- programmable custom glyphs
- brightness control
- safe four-channel fan control
- LibreHardwareMonitor integration
- first-class Winamp integration
- optional Home Assistant, Plex, OctoPrint and server-monitoring sources
- tray operation and Windows autostart
- graphical page designer and profiles
- virtual LIS2 transport for development and testing
- protocol diagnostics and logging

## Development

The first implementation milestone is `tools/LIS2.ProtocolTester`, used to validate the reverse-engineered protocol against real hardware before freezing the low-level `LIS2.Core` API.

Detailed protocol, architecture and reverse-engineering documentation lives in `docs/`.

## Copyright / reverse engineering

This repository intentionally does **not** contain MCC binaries, original VL System installers, proprietary VL System software or Winamp binaries. It contains independently written source code, interoperability documentation, protocol notes and project-created test vectors.

## Guiding principle

> **Preserve the hardware. Replace the limitations of its software.**
