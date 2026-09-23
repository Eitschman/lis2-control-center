# LIS2 Control Center — Project Specification

## Project

**LIS2 Control Center** is a modern Windows control application for the **VL System L.I.S. 2** VFD display and four-channel fan controller.

- Status: Planning / Reverse Engineering
- Target: Windows 11 x64
- Primary implementation: C# / .NET 8 or newer
- Repository: `Eitschman/lis2-control-center`
- License: MIT

## Mission

The project preserves useful LIS2 hardware while replacing the limitations of its obsolete original software.

It is not a compatibility wrapper around MCC. The goal is an independent implementation providing:

1. native VL System LIS2 support;
2. Windows 11 support;
3. no runtime dependency on MCC;
4. safe four-channel fan control;
5. flexible 20x2 rendering;
6. programmable custom characters;
7. first-class Winamp support;
8. hardware monitoring;
9. event-driven pages;
10. optional Home Assistant, Plex, OctoPrint and server monitoring;
11. modular data sources;
12. graphical page design;
13. profiles;
14. tray operation and autostart;
15. protocol documentation.

## Hardware model

Target LIS2 hardware provides:

- 20x2 character VFD
- four independently controllable fan channels
- eight programmable display characters
- serial host communication

The initial transport configuration is 19200 baud, 8N1, no handshake.

## Protocol strategy

Protocol knowledge is classified as CONFIRMED, MCC-CONFIRMED, SUPPORTED, EXPERIMENTAL or UNKNOWN.

The first implementation milestone is a dedicated protocol tester. Its purpose is to validate the currently reverse-engineered behavior on real LIS2 hardware before the low-level production API is frozen.

See `protocol.md` for byte-level details.

## Software architecture

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
Serialized Command Queue
    |
    v
Serial Transport
    |
    v
VL System L.I.S. 2
```

Planned solution projects:

```text
src/
  LIS2.Core/
  LIS2.Display/
  LIS2.Sources/
  LIS2.App/
  LIS2.Winamp/

tests/
  LIS2.Core.Tests/
  LIS2.Display.Tests/

tools/
  LIS2.ProtocolTester/
```

### LIS2.Core

Owns:

- serial transport
- protocol encoding
- connection and error state
- display writes
- brightness
- custom characters
- fan commands

No other project writes directly to the COM port.

### Command queue

Display updates, fan commands and custom-character programming all use one serialized command queue. This prevents interleaved protocol packets.

### Display engine

The display engine renders a logical 20x2 canvas.

Features:

- templates
- variables
- clipping
- padding
- alignment
- scrolling
- page rotation
- custom glyphs
- conditions
- event overlays
- differential updates

Every rendered line is exactly 20 cells.

### Data sources

Data providers expose normalized values without knowledge of LIS2 protocol details.

Sources may use:

- polling
- Windows APIs
- events
- HTTP
- WebSockets
- MQTT
- named pipes
- files
- process output

## Pages and scheduling

Pages have:

- name
- line templates
- duration
- visibility condition
- scrolling behavior
- priority
- optional custom-glyph set
- profile membership

A Winamp page, for example, can exist only while playback is active.

### Event pages

Events may temporarily interrupt normal page rotation.

Priority model:

- INFO
- MEDIA
- NOTICE
- WARNING
- CRITICAL

CRITICAL has the highest priority. Safety-relevant warnings may interrupt media pages.

## Custom characters

Eight programmable glyph slots are managed centrally.

A `CustomCharacterManager` should:

- track current slot assignments;
- avoid unnecessary reprogramming;
- switch glyph sets when required;
- prevent slot conflicts.

Potential built-in glyph sets include:

- bar graphs
- VU meters
- play/pause
- arrows
- temperature
- network activity
- progress indicators

## Fan controller

Fan control is independent of display rendering and network integrations.

Planned modes:

- Fixed
- Curve
- Follow
- External
- Off

Per-channel safety configuration includes:

- minimum output
- maximum output
- fail-safe output
- sensor timeout
- permission to stop the fan

Recommended initial defaults:

- minimum 30%
- maximum 100%
- fail-safe 100%
- missing/invalid sensor => 100%
- controller error => 100%
- stop permission false

Curve mode uses interpolation between configured temperature/output points.

## Winamp integration

Winamp is a first-class feature rather than a generic afterthought.

Desired values include:

- playback state
- artist
- title
- album
- year
- genre
- track number
- playlist position/count
- elapsed time
- duration
- bitrate
- sample rate
- channel mode
- file/stream title

Classic `WM_WA_IPC` can provide useful control and information, but pointer-returning IPC operations are unsafe across process boundaries without explicit memory handling.

Preferred architecture:

```text
Winamp
  |
  v
gen_lis2.dll
  |
  | Windows Named Pipe
  v
LIS2 Control Center
```

Preferred pipe:

```text
\\.\pipe\LIS2ControlCenter.Winamp
```

The plugin gathers native Winamp metadata/events and sends normalized data. It does not control LIS2 hardware.

Events include:

- Winamp started/stopped
- playback started/paused/stopped
- track changed
- stream title changed
- playlist changed

Track changes may trigger temporary display event pages.

## Hardware monitoring

Initial backend: **LibreHardwareMonitor**.

Desired normalized variables include CPU/GPU load and temperature, clocks, memory, RAM, storage/motherboard temperatures, fan speeds and voltages where available.

A shared sensor registry serves both display sources and fan control. Failure of display rendering or an optional source must never disable fan safety.

## Optional integrations

### Home Assistant

Potential transports:

- REST
- WebSocket
- MQTT

WebSocket is preferred for frequently changing states. Users map arbitrary entities to display variables.

A future option may export LIS2 state back to Home Assistant.

### Plex

Possible values include server/playback/media information. Media-source priority must be configurable, for example Winamp playback before Plex playback.

### OctoPrint

Potential values:

- printer state
- filename
- progress
- elapsed/remaining time
- temperatures

Print completion can generate an event page.

### Docker / server monitoring

Prefer generic APIs over hard coupling to one product. Possible sources include Docker Engine API, Komodo, Beszel or generic HTTP/webhook sources.

### Generic custom sources

Potential built-in source types:

- HTTP JSON
- MQTT
- process output
- text/JSON file
- named pipe
- webhook

Command execution requires explicit security consideration.

A public source-plugin API may be added later.

## Windows application

The initial product is an interactive user-session tray application, not a Windows service.

The tray remains active when the configuration window is closed.

Potential tray actions:

- connection state
- current page
- open configuration
- pause page rotation
- previous/next page
- fan fail-safe
- display off
- reconnect
- exit

Planned main navigation:

- Dashboard
- Display
- Pages
- Winamp
- Hardware
- Fan Control
- Home Assistant
- Plex
- OctoPrint
- Docker
- Custom Sources
- Events
- Device
- Settings
- Diagnostics

The dashboard includes a simulated VFD whose contents exactly match renderer output.

UI direction:

- modern
- clean
- compact
- dark-mode capable
- high information density
- minimal unnecessary animation

The retro element should be the physical VFD, not an imitation Windows XP interface.

## Profiles

Example profiles:

- Normal
- Music
- 3D Printing

Possible automatic activation:

- Winamp playing => Music
- OctoPrint printing => 3D Printing
- otherwise => Normal

Explicit priority rules must prevent profile oscillation.

## Configuration and persistence

JSON is sufficient initially.

SQLite may later store pages, profiles, fan curves, events, plugin/source configuration or history.

A hybrid model is possible:

- `settings.json`
- `lis2.db`

Persistence must remain decoupled from UI implementation.

## Error handling

Expected failure cases include:

- COM port disappears
- USB/serial adapter disconnects
- COM port is occupied
- LIS2 loses power
- Winamp exits
- Home Assistant/Plex/OctoPrint becomes unavailable
- hardware sensor disappears
- template evaluation fails

Display-source failures must not crash fan control.

### Connection state machine

```text
Disconnected
 -> Connecting
 -> Connected
 -> Faulted
 -> WaitingForReconnect
 -> Connecting
```

After reconnect, restore safe fan state first, then brightness, glyph state, display state and normal fan regulation.

## Diagnostics

Diagnostics should expose:

- connection state
- COM port
- last write
- last error
- current rendered frame
- current custom glyph set
- fan state
- data-source health
- recent events
- optional protocol log

Diagnostic exports must redact secrets.

## Security

Tokens for integrations such as Home Assistant, Plex or OctoPrint must not appear as plain text in exported configuration.

Consider Windows DPAPI or another Windows credential mechanism for secret storage.

Logs and diagnostic exports must redact credentials.

## Testing

Most software should be testable without physical LIS2 hardware.

### Protocol tests

Examples:

```text
SetFans(50, 50, 75, 100)
=> AE F0 32 32 4B 64
```

```text
Write line 1, column 0, "ABC"
=> A1 00 A7 41 42 43
```

### Renderer tests

Every rendered line must contain exactly 20 cells.

Test clipping, padding, alignment, variable expansion and scrolling.

### Fan tests

Test:

- interpolation
- min/max limits
- fail-safe
- invalid sensor data
- sensor timeout
- clamping

### Virtual LIS2

A virtual transport should simulate the VFD, brightness, custom characters, fans and connection state so development can continue without hardware.

## Protocol Tester

`tools/LIS2.ProtocolTester` is built before the full application.

Initial capabilities:

- COM port selection
- connect/disconnect
- clear
- line text
- brightness
- fan outputs
- one custom character
- raw TX log

Only sufficiently confident commands are enabled by default. Experimental commands require explicit opt-in.

The tester remains in the repository as a diagnostic tool after the main application exists.

## Hardware validation sequence

1. Open 19200 8N1 and send nothing.
2. Send `A0`; observe.
3. Send `A1 00 A7 48 45 4C 4C 4F`; expect HELLO on line 1.
4. Validate line 2.
5. Validate brightness.
6. Validate one custom character.
7. Validate fan control, beginning at 100% and reducing modestly.

Do not initially test 0% on cooling-critical fan channels.

## Development phases

### Phase 0 — Documentation

- repository
- README
- protocol documentation
- architecture
- reverse-engineering notes

### Phase 1 — Protocol Tester

- COM handling
- 19200 8N1
- clear
- text
- brightness
- fans
- custom character
- TX log

### Phase 2 — LIS2.Core

- transport abstraction
- protocol encoder
- serialized queue
- device API
- reconnection
- virtual transport
- protocol tests

### Phase 3 — Display Engine

- 20x2 renderer
- templates
- variables
- pages
- scrolling
- differential updates
- glyph management

### Phase 4 — Basic Application

- tray
- settings
- dashboard
- autostart
- reconnect

### Phase 5 — Hardware Monitoring

- LibreHardwareMonitor
- sensor registry
- display variables

### Phase 6 — Fan Controller

- fixed/curve/follow modes
- safety limits
- fail-safe
- diagnostics

### Phase 7 — Winamp

- IPC investigation
- `gen_lis2.dll`
- named pipe
- metadata/events
- progress

### Phase 8 — Event Engine

- priorities
- overlays
- expiration
- interruption rules

### Phase 9 — Integrations

- Home Assistant
- Plex
- OctoPrint
- Docker/server
- generic API sources

### Phase 10 — Designer / Profiles

- graphical page designer
- profile editor
- automatic profile selection

### Phase 11 — Packaging

- installer
- update strategy
- releases
- user documentation

## Version roadmap

- v0.1 — Hardware Bring-Up
- v0.2 — Core Driver
- v0.3 — Display Engine
- v0.4 — Hardware Monitoring
- v0.5 — Fan Controller
- v0.6 — Winamp
- v0.7 — Events
- v0.8 — Integrations
- v0.9 — Designer
- v1.0 — Stable

## Open questions

Protocol:

- Is the full-line zero terminator required?
- What exactly does `A0` reset?
- How is brightness OFF encoded?
- What is `AB 00 ...`?
- Does LIS2 return any response?
- Is safe automatic identification possible?
- What character ROM/code page is used?
- What happens beyond column 19?
- Is firmware version information readable?
- Do low fan values have special semantics?

Hardware:

- What USB/serial adapter identity is seen on the target machine?
- What happens if LIS2 loses power while COM remains?
- How reliably can the port be reopened?
- Are fan settings preserved after disconnect?
- What fan state follows reset?

These questions belong to Phase 1 rather than being guessed in production code.

## Current project state

At development start:

- MCC static analysis is complete enough to begin hardware validation.
- Major display, fan and custom-character commands are identified.
- LIS2 and MPlay evidence has been separated.
- Brightness commands are strongly supported.
- misleading/unresolved byte sequences are rejected or quarantined.
- automatic device detection remains unresolved but is not blocking.
- application architecture is defined.
- Winamp is a first-class requirement.
- C# / .NET is the selected implementation direction.

The immediate engineering milestone is **LIS2.ProtocolTester**, followed by freezing the validated low-level protocol API and beginning `LIS2.Core`.

## Guiding principle

> **Preserve the hardware. Replace the limitations of its software.**
