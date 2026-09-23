# Architecture

## Core principle

LIS2 Control Center separates data acquisition, rendering, device control and serial transport.

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

Only `LIS2.Core` knows protocol bytes such as `0xA1`, `0xAB` or `0xAE`.

## Planned projects

- `LIS2.Core` — serial transport, protocol encoding, connection state, display commands, brightness, custom characters and fans.
- `LIS2.Display` — logical 20x2 canvas, templates, variables, clipping, alignment, scrolling, pages and event overlays.
- `LIS2.Sources` — normalized data-provider abstractions and built-in sources.
- `LIS2.App` — Windows UI, tray application, settings, profiles and diagnostics.
- `LIS2.Winamp` — host-side Winamp integration.
- `LIS2.ProtocolTester` — focused hardware bring-up and protocol-validation tool.

## Device abstraction

The intended device API should expose operations conceptually equivalent to:

```csharp
bool IsConnected { get; }

Task ConnectAsync(...);
Task DisconnectAsync();

Task ClearAsync();
Task WriteLineAsync(...);
Task WriteAsync(...);
Task SetBrightnessAsync(...);
Task SetFansAsync(...);
Task ProgramCharacterAsync(...);
```

The public API must express device semantics rather than raw command bytes.

## Serialized hardware access

All serial writes pass through one serialized command queue. Display rendering, fan control and custom-character management must never write independently to the COM port.

Commands should be atomic from the perspective of the queue.

Optional protocol logging records both hexadecimal traffic and semantic intent.

## Display engine

The renderer owns a logical 20x2 character canvas and supports:

- templates and variables
- clipping and padding
- alignment
- scrolling
- page rotation
- custom characters
- event overlays
- visibility conditions
- differential updates

A rendered line is always exactly 20 cells.

Initial implementations may rewrite complete lines. Later versions should send only changed spans when safe.

## Data sources

Data providers expose normalized values and know nothing about the serial protocol.

A conceptual interface:

```csharp
interface IDataSource
{
    string Id { get; }
    Task StartAsync(...);
    Task StopAsync(...);
    IReadOnlyDictionary<string, object?> Values { get; }
    event EventHandler? Changed;
}
```

Providers may internally use polling, events, WebSockets, named pipes, HTTP or Windows APIs.

## Fan controller

Fan regulation is a safety-critical subsystem and remains independent from display rendering.

Planned modes:

- Fixed
- Curve
- Follow
- External
- Off (explicitly permitted only)

Sensor loss, invalid values or controller errors immediately invoke fail-safe output.

## Winamp

Winamp is a first-class integration.

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

The plugin collects normalized metadata and playback events. It does not control LIS2 hardware.

## Hardware monitoring

LibreHardwareMonitor is the initial hardware sensor backend.

A shared sensor registry feeds both display variables and fan control, while preserving separation between display failures and fan safety.

## Application model

The initial application runs in the interactive Windows user session as a tray application rather than a Windows service.

Reasons include:

- Winamp integration
- user-session APIs
- hardware-monitoring behavior
- avoiding Session 0 complexity

Closing the configuration window does not terminate the controller.

## Reconnection

Connection lifecycle:

```text
Disconnected
    -> Connecting
    -> Connected
    -> Faulted
    -> WaitingForReconnect
    -> Connecting
```

After reconnect, restore state in a controlled order with fan safety taking priority:

1. safe fan state
2. brightness
3. custom glyph set
4. display frame
5. normal fan regulation

## Virtual LIS2

A virtual transport must exist alongside the serial transport. It should simulate:

- 20x2 VFD contents
- brightness
- custom glyph slots
- four fan outputs
- connection/fault behavior

This enables most development and automated testing without physical hardware.
