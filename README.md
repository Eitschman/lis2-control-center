# LIS2 Control Center

Modern Windows control software for the **VL System L.I.S. 2** 20x2 VFD display and four-channel fan controller.

> **Status:** Active development — the main application is already broadly usable  
> **Target:** Windows 11 x64  
> **Implementation:** C# / .NET 8, WPF, native Win32 Winamp plug-in  
> **License:** MIT

LIS2 Control Center is an independent replacement for the obsolete VL System Multimedia Control Center (MCC). It combines direct LIS2 hardware control with programmable display pages, PC hardware monitoring, Winamp telemetry, Home Assistant data and fan control.

> **Preserve the hardware. Replace the limitations of its software.**

## What works today

### LIS2 / display

- serial LIS2 transport at 19200 baud, 8N1 plus a virtual transport
- 20x2 VFD output, preview, clear and 25/50/75/100% brightness
- differential display updates: only changed spans are written after the first frame
- native uPD16314 character mapping, including German umlauts, `ß`, `°`, `µ`, arrows and useful mathematical symbols
- raw character-ROM test support for hardware validation
- eight programmable 5x8 custom-character slots
- graphical CG Builder with live preview, send-one/send-all and project-owned glyph-set files
- named custom glyph references in page templates
- built-in glyph sets, including the eight-level Winamp spectrum set

### Pages and events

- configurable page sequence with add, duplicate, reorder, enable/disable and per-page duration
- two independent line templates with live editor preview
- per-line overflow mode: **PingPong**, **Marquee** or **Truncate**
- configurable scroll speed and edge pause
- simple source-value visibility expressions such as `Winamp.State=Playing`
- scheduler-driven rotation independent from scrolling animation
- event/overlay queue with priority and lifetime plus UI/test generation
- ready-made presets for Clock, Status, CPU, GPU, Memory, Temperatures, Fans, Winamp Now Playing, VU and Spectrum

### PC hardware and fan control

- LibreHardwareMonitor source
- searchable/filterable hardware browser grouped by device
- aliases and favorites
- configured-but-missing sensors remain visible as unavailable
- direct assignment of sensors to fan channels
- four fan channels with **Fixed, Curve, Follow, External and Off** modes
- minimum/maximum/fail-safe output
- missing/invalid sensor fail-safe handling
- fan-curve editor/preview, live input/output display and optional hysteresis

### Winamp

- native 32-bit `gen_lis2.dll` general-purpose plug-in
- JSON snapshots over `\\.\pipe\LIS2ControlCenter.Winamp`
- artist, title, album, playback state, elapsed/duration, playlist position/count, bitrate and sample rate
- VU telemetry
- 20-band spectrum telemetry and eight-level custom-glyph VFD spectrum
- stale/disconnect handling
- Winamp simulator for development without Winamp

### Home Assistant

Home Assistant is a first-class read-only data source using the WebSocket API rather than polling.

- long-lived access-token authentication
- initial `get_states` plus live `state_changed` subscription
- automatic reconnect
- searchable entity browser with domain filter
- aliases and favorites
- state, unit and friendly-name template values
- **all entity attributes exposed as template values**
- attribute browser and direct page creation from an entity or attribute

Examples:

```text
{HA.sensor.server_cpu}
{HA.sensor.server_disk.attribute.free}
{HA.DockerDisk.attribute.total}
```

This makes Home Assistant the **universal integration layer** for data that is already available there. Weather/DWD, OctoPrint, Plex, Beszel/server and Docker monitoring, network devices, Zigbee sensors and other HA integrations can be displayed without implementing dedicated LIS2 data sources for each system.

Dedicated LIS2 integrations are therefore only useful when they provide a concrete capability that Home Assistant cannot provide adequately. In particular, a separate OctoPrint or generic Docker/server-monitoring source is not planned while those values are already available through HA.

See [docs/home-assistant-integration.md](docs/home-assistant-integration.md).

### Windows application

- tray operation and Windows autostart
- Light, Dark and Windows-system themes
- runtime localization: System, English, German, French, Turkish and Russian with English fallback
- application/version/about information
- persistent JSON settings under the user's local application data
- custom LIS2 application/tray/taskbar icon
- diagnostics/logging
- coalesced asynchronous display rendering to prevent overlapping writes

## Architecture

```text
Clock / Hardware / Winamp / Home Assistant
                  |
                  v
          DataSourceRegistry
                  |
                  v
      Display runtime / events
                  |
                  v
        DisplayFrameWriter
                  |
                  v
            Lis2Device
             /     \
            v       v
        Serial    Virtual
```

The projects are deliberately separated:

- `src/LIS2.Core` — protocol encoding, character mapping, device abstraction, serial/virtual transports and custom-character manager
- `src/LIS2.Display` — frames, templates, scheduler, scrolling, visibility, events and differential writes
- `src/LIS2.Sources` — clock, LibreHardwareMonitor and Home Assistant sources plus source registry
- `src/LIS2.Fans` — fan-control modes, curves, hysteresis and fail-safe logic
- `src/LIS2.Winamp` — named-pipe host, Winamp snapshots and display-value formatting
- `src/LIS2.App` — WPF UI, settings, pages, presets, tray, themes and localization
- `tools/LIS2.ProtocolTester` — low-level protocol testing
- `tools/LIS2.WinampSimulator` — Winamp testing without Winamp
- `native/LIS2.WinampPlugin` — native x86 Winamp plug-in

## Display variables

Data sources are source-agnostic from the display engine's point of view. Typical variables include:

```text
{Clock.Time}
{Clock.Date}

{Hardware.<sensor-key>}

{Winamp.Artist}
{Winamp.Title}
{Winamp.State}
{Winamp.Vu}
{Winamp.SpectrumGlyphs}

{HA.sensor.example}
{HA.sensor.example.Unit}
{HA.sensor.example.attribute.some_attribute}
```

Aliases provide shorter stable names for hardware/Home Assistant values where configured.

## Protocol status

The production implementation currently covers:

- `A0` clear/reset
- `A1/A2 <column> A7 ...` line writes
- `A5 38/39/3A/3B` brightness
- `AE F0 ...` four fan outputs
- `AB <slot> <row> <bitmap>` custom-character programming
- uPD16314 ROM-code-002 Unicode/display-byte conversion

Some protocol details remain intentionally unresolved until verified on physical hardware, including brightness-off behavior, the significance of MCC's trailing `00`, safe automatic device identification and some custom-character details.

See [docs/protocol.md](docs/protocol.md) and [docs/reverse-engineering.md](docs/reverse-engineering.md).

## Building

Requirements for a normal development build:

- Windows
- .NET 8 SDK
- Visual Studio 2022 or another environment capable of building WPF
- CMake/MSVC when building the native Winamp plug-in

```powershell
dotnet restore .\LIS2ControlCenter.sln
dotnet build .\LIS2ControlCenter.sln -c Release
dotnet test .\LIS2ControlCenter.sln -c Release --no-build
```

## CI and development artifacts

GitHub Actions currently:

1. restores and builds the complete .NET solution on Windows;
2. runs the Core, Display, Sources, Fans and Winamp tests;
3. runs a WPF startup/tab smoke test;
4. publishes a self-contained Windows x64 LIS2 Control Center;
5. publishes the self-contained x64 protocol tester;
6. builds and uploads the native Winamp x86 plug-in.

Artifacts:

- `LIS2-Control-Center-win-x64`
- `LIS2-ProtocolTester-win-x64`
- `gen_lis2-win32`

## Documentation

- [Project specification](docs/PROJECT_SPEC.md)
- [Architecture](docs/architecture.md)
- [Protocol notes](docs/protocol.md)
- [Reverse-engineering notes](docs/reverse-engineering.md)
- [Winamp integration](docs/winamp-integration.md)
- [Home Assistant integration](docs/home-assistant-integration.md)
- [Roadmap / TODO](TODO.md)

## Scope decisions

Several ideas have deliberately **not** been added:

- no settings import/export
- no restore-defaults workflow
- no first-run wizard
- no direct LIS2 hardware access from the Winamp plug-in
- Home Assistant is currently read-only
- no dedicated OctoPrint source: OctoPrint values already available in Home Assistant are consumed through the HA source
- no dedicated Plex source: Plex values already available in Home Assistant are consumed through the HA source
- no duplicate Docker/server-monitoring source solely for values already available through Home Assistant
- unknown/experimental protocol commands are never sent automatically

The application is not intended to clone MCC pixel-for-pixel. Historical MCC behavior is used where it helps interoperability or preserves a useful capability; the application architecture and UI are otherwise independent.

## Copyright / reverse engineering

This repository does **not** contain MCC binaries, original VL System installers, proprietary VL System software, proprietary artwork or Winamp binaries.

It contains independently written source code, interoperability documentation, protocol notes and project-created test vectors. Reverse-engineering findings are documented for interoperability and hardware preservation.
