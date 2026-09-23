# LIS2 Control Center

Modern Windows control software for the **VL System L.I.S. 2** VFD display and four-channel fan controller.

> **Status:** Active development — usable development builds available  
> **Target:** Windows 11 x64  
> **Implementation:** C# / .NET 8  
> **License:** MIT

## Vision

LIS2 Control Center turns the VL System L.I.S. 2 into a modern programmable Windows information display, Winamp/media display, hardware-monitoring frontend and safe four-channel fan controller.

The project replaces the obsolete VL System Multimedia Control Center (MCC) with an independent implementation based on interoperability research, analysis of the original software and real-hardware validation.

> **Preserve the hardware. Replace the limitations of its software.**

## Hardware

The target device provides:

- 20x2 character VFD
- four independently controllable fan channels
- eight programmable custom characters
- serial communication at 19200 baud, 8N1
- brightness control

The application can also run without an attached LIS2 by using its virtual transport, which is useful for development and UI testing.

## Current state

The project has moved well beyond the original planning/protocol-test stage. The Windows application currently includes:

- serial and virtual LIS2 transports
- 20x2 VFD preview and direct display output
- brightness control
- configurable rotating display pages
- template variables backed by data sources
- display event/runtime infrastructure
- LibreHardwareMonitor integration
- live hardware sensor browser with search and type filtering
- direct assignment of hardware sensors to fan channels
- four-channel fan controller with fixed, curve and follow modes
- configurable minimum, maximum and fail-safe fan output
- native 32-bit Winamp plugin using a named pipe
- live Winamp Now Playing view
- Winamp title, artist, album, playback, timing, playlist and audio metadata
- Winamp simulator for development without Winamp
- Windows tray operation and autostart
- Dark, Light and Windows-system themes
- protocol diagnostics and logging
- protocol tester
- virtual LIS2 protocol/state simulation
- automated Windows builds and tests through GitHub Actions

## Architecture

The main data path is deliberately layered:

```text
Data Sources
    |
    v
Display Engine
    |
    v
LIS2 Device Abstraction
    |
    v
Serial / Virtual Transport
    |
    v
VL System L.I.S. 2
```

Only the low-level LIS2 layer needs to know the device protocol bytes. Display pages work with named values such as hardware or Winamp data instead of protocol commands.

Major projects:

- `LIS2.Core` — protocol, device abstraction and transports
- `LIS2.Display` — frames, templates, page scheduling and events
- `LIS2.Sources` — generic data-source infrastructure and hardware monitoring
- `LIS2.Fans` — fan-control logic and safety handling
- `LIS2.Winamp` — Winamp named-pipe host and media data source
- `LIS2.App` — Windows/WPF application
- `LIS2.ProtocolTester` — low-level protocol testing
- `LIS2.WinampSimulator` — Winamp integration testing without Winamp
- `native/LIS2.WinampPlugin` — 32-bit `gen_lis2.dll` for classic Winamp

Detailed protocol, architecture and reverse-engineering documentation lives in `docs/`.

## Winamp

Winamp is a first-class integration rather than a screen-scraping workaround.

The native 32-bit general-purpose plugin runs inside classic Winamp and sends metadata/events to LIS2 Control Center through:

```text
\\.\pipe\LIS2ControlCenter.Winamp
```

The Windows application exposes values including state, artist, title, album, elapsed time, duration, playlist position/count, bitrate and sample rate to the display system.

## Hardware monitoring and fan control

LibreHardwareMonitor supplies CPU, GPU, motherboard, storage, memory and fan-related values where supported by the machine.

The Hardware page provides a live searchable sensor list. Sensors can be used by display templates and assigned directly to fan channels.

Fan control is intentionally safety-oriented. Each channel supports configurable minimum, maximum and fail-safe output. Missing or invalid sensor data must never silently result in an unsafe cooling state.

## Roadmap

The next feature work is tracked in [TODO.md](TODO.md). Items derived from surviving documentation/screenshots of the original MCC are explicitly included there, particularly:

- richer Auto User/page-sequence editing
- hardware/display presets
- original-style fan safety guidance
- scrolling/marquee support for long text
- graphical custom-character (CG) builder
- optional Winamp VU/spectrum visualization

The new application is not intended to reproduce MCC pixel-for-pixel. Useful original capabilities are preserved while obsolete limitations are replaced with a more flexible architecture.

## Protocol status

Confirmed/supported LIS2 functionality includes display output, brightness levels, fan output and custom-character commands. Some details of the original protocol remain deliberately marked experimental or unknown until they can be validated against real hardware.

In particular, LIS2 and related MPlay command families are kept separate; unknown commands are not sent automatically.

See:

- `docs/protocol.md`
- `docs/reverse-engineering.md`
- `docs/PROJECT_SPEC.md`

## Development builds

GitHub Actions validates changes on Windows.

Successful builds on `main` (and manual workflow runs) produce downloadable artifacts:

- `LIS2-Control-Center-win-x64` — self-contained Windows x64 application
- `LIS2-ProtocolTester-win-x64` — self-contained Windows x64 protocol tester
- `gen_lis2-win32` — native 32-bit Winamp general-purpose plugin

The .NET artifacts are self-contained and do not require a separate .NET 8 runtime installation.

The Winamp plugin is intentionally 32-bit because it is loaded into classic Winamp's process.

## Copyright / reverse engineering

This repository intentionally does **not** contain MCC binaries, original VL System installers, proprietary VL System software or Winamp binaries.

It contains independently written source code, interoperability documentation, protocol notes and project-created test vectors. Historical reviews, screenshots and surviving documentation may be used as research references, but proprietary assets are not redistributed.
