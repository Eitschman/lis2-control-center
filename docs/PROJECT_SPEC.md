# LIS2 Control Center — Project Specification

## Project status

**LIS2 Control Center** is an actively developed Windows 11 control application for the **VL System L.I.S. 2** 20x2 VFD display and four-channel fan controller.

- Repository: `Eitschman/lis2-control-center`
- Target: Windows 11 x64
- Main application: C# / .NET 8 / WPF
- Winamp plug-in: native Win32/x86
- License: MIT
- State: functional development application; physical-hardware validation is still ongoing for some protocol details

The project is no longer in its original planning/protocol-tester phase. Serial/virtual LIS2 control, display pages, hardware monitoring, fan control, Winamp and Home Assistant are implemented.

## Mission

Preserve useful LIS2 hardware while replacing the obsolete MCC software with an independent, maintainable implementation.

Core principles:

1. no runtime dependency on MCC;
2. isolate protocol details behind the LIS2 device layer;
3. keep display/data-source integrations independent from fan safety;
4. support real and virtual hardware;
5. document uncertain protocol behavior instead of guessing;
6. never send unknown commands automatically;
7. prefer reusable data-source abstractions over one-off display integrations.

## Hardware model

The target LIS2 provides:

- 20x2 character VFD
- four fan outputs
- eight programmable 5x8 custom characters
- serial host communication at 19200 baud, 8N1
- brightness control

The display character path uses the NEC uPD16314 ROM-code-002 mapping currently documented in `protocol.md`.

## Implemented solution structure

```text
src/
  LIS2.Core/
  LIS2.Display/
  LIS2.Sources/
  LIS2.Fans/
  LIS2.Winamp/
  LIS2.App/

tests/
  LIS2.Core.Tests/
  LIS2.Display.Tests/
  LIS2.Sources.Tests/
  LIS2.Fans.Tests/
  LIS2.Winamp.Tests/

tools/
  LIS2.ProtocolTester/
  LIS2.WinampSimulator/

native/
  LIS2.WinampPlugin/
```

### LIS2.Core

Owns:

- protocol encoding
- Unicode/native display-character mapping
- serial and virtual transports
- device abstraction
- serialized device writes
- brightness/fan/display/custom-character commands
- raw display-byte testing
- `CustomCharacterManager`

No data source or media integration writes directly to the COM port.

### LIS2.Display

Owns:

- exact 20x2 `DisplayFrame`
- template rendering
- page scheduling
- PingPong/Marquee/Truncate overflow behavior
- visibility evaluation
- event/overlay queue
- differential changed-span writes
- coalescing support for asynchronous render requests

### LIS2.Sources

Current sources:

- Clock
- LibreHardwareMonitor
- Home Assistant WebSocket API

`DataSourceRegistry` combines source snapshots into namespaced display values.

### LIS2.Fans

Owns fan regulation independently from display rendering.

Implemented modes:

- Fixed
- Curve
- Follow
- External
- Off

The controller supports min/max/fail-safe values and hysteresis. Invalid/missing sensor input invokes fail-safe behavior where applicable.

### LIS2.Winamp

Owns the Windows named-pipe server, Winamp snapshots and display-value formatting.

The native x86 `gen_lis2.dll` runs inside classic Winamp and publishes normalized metadata, playback state, VU and spectrum telemetry. It never controls LIS2 hardware.

### LIS2.App

The WPF application owns:

- configuration and persisted settings
- transport selection
- display/page editor and presets
- custom-character builder
- events UI
- hardware browser
- fan-control UI
- Winamp UI
- Home Assistant browser/attributes
- tray/autostart
- themes/localization
- About/version and diagnostics

## Runtime architecture

```text
Clock ───────────────┐
LibreHardwareMonitor ├─> DataSourceRegistry ─┐
Winamp named pipe ───┤                       │
Home Assistant WS ───┘                       v
                                      DisplayRuntime
                                   / scheduler + events
                                             |
                                             v
                                     DisplayFrameWriter
                                             |
                                             v
                                         Lis2Device
                                        /          \
                                  SerialLis2     VirtualLis2
```

Fan control consumes sensor values separately and sends fan commands through the same LIS2 device abstraction.

## Display pages

A persisted page currently contains:

- ID/name
- line 1/line 2 templates
- duration
- enabled flag
- priority
- optional `key=value` visibility expression
- independent overflow mode for each line
- scroll step interval
- edge-pause interval

The editor supports:

- add
- duplicate
- reorder
- enable/disable
- live preview
- presets

Implemented presets:

- Clock & date
- LIS2 Status
- CPU
- GPU
- Memory
- Temperatures
- Fans
- Winamp Now Playing
- Winamp VU
- Winamp Spectrum

## Events

`EventQueue` overlays temporary frames over normal page rotation. Events have priority and expiry/lifetime. The application includes UI for managing/testing events.

## Custom characters

All eight slots are managed centrally.

Implemented:

- 5x8 graphical editor
- per-slot and all-slot programming
- live preview
- cached programming to avoid unchanged writes
- project-owned glyph-set files
- named template references
- built-in glyph sets
- eight-level Winamp spectrum glyph set

Physical validation of every slot/operation remains an explicit protocol task.

## Hardware monitoring

LibreHardwareMonitor supplies local machine sensor data.

The UI provides:

- search/filter
- grouping by hardware
- value/unit/template key
- aliases/favorites
- persistence
- unavailable representation for configured sensors that disappear
- direct fan-channel assignment
- ready-made display presets

## Winamp integration

```text
Winamp (x86)
    |
gen_lis2.dll
    |
JSON over \\.\pipe\LIS2ControlCenter.Winamp
    |
LIS2.Winamp
    |
DataSourceRegistry
```

Telemetry includes metadata, playback/timing/playlist information, VU and a 20-band analyzer spectrum.

The spectrum page uses all eight custom-character slots as vertical bar levels.

## Home Assistant integration

The HA source is read-only and event-driven.

Flow:

1. connect to `/api/websocket`;
2. authenticate with a long-lived token;
3. fetch `get_states`;
4. subscribe to `state_changed`;
5. publish entity states and attributes into the source registry;
6. reconnect automatically after disconnect.

The application includes:

- entity search/domain filter
- aliases/favorites
- page creation from an entity
- attribute browser
- page creation from an attribute

Attributes use:

```text
{HA.<entity_id>.attribute.<attribute_name>}
```

Aliases also propagate to attribute variables.

## Application model

The application runs in the interactive Windows session and supports tray operation rather than a Windows service. This is appropriate for Winamp integration, WPF UI and user-session hardware monitoring.

Settings are persisted as JSON in the user's local application-data directory.

Deliberate UX decisions:

- no first-run wizard
- no settings import/export
- no restore-defaults workflow

## Quality / CI

GitHub Actions on Windows currently:

- restores/builds the .NET solution
- runs all test projects
- runs a WPF startup/tab smoke test
- publishes self-contained x64 application and protocol-tester artifacts
- builds the native x86 Winamp plug-in

The display render path is serialized/coalesced to prevent overlapping asynchronous writes.

## Remaining scope

The authoritative remaining work is tracked in `TODO.md`. The main themes are now:

- physical LIS2 protocol validation
- fan-safety UX/real-hardware validation
- formatting/fallback helpers for display variables
- continued theme/layout polish
- diagnostics improvements
- selected engineering tests
- optional future data sources only where they add value beyond Home Assistant

This document describes the implemented architecture. Byte-level confidence and unresolved protocol questions remain in `protocol.md` and `reverse-engineering.md`.
