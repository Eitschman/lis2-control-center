# LIS2 Control Center: Projektspezifikation

## Project status

**LIS2 Control Center** steuert das **VL System L.I.S. 2** mit 20x2-VFD und vier Lüfterkanälen. Auf `feature/linux-avalonia` wird eine gemeinsame Windows-/Linux-App entwickelt. Die WPF-App bleibt bis zum Paritätsnachweis die Referenz.

- Repository: `Eitschman/lis2-control-center`
- Zielplattformen: Windows x64, Linux x64 und Linux ARM64.
- Gemeinsame Anwendung: C# / .NET 8 / Avalonia 11.3.6.
- Referenzanwendung: Windows-WPF unter `src/LIS2.App`.
- Winamp plug-in: native Win32/x86
- License: MIT
- Implementierungsstand: PORT-01, Stand 20.10.2026; Prüfnachweise im [Status](prp/linux-port-status.md). Physische LIS2-Prüfung bleibt offen.

Das [Avalonia-PRP](prp/linux-avalonia-port.md) bestimmt das Portierungsziel. [Status](prp/linux-port-status.md) und [Paritätsaudit](prp/wpf-avalonia-parity-audit.md) unterscheiden vorhandenen Code, automatische Nachweise und offene Oberflächen-/Hardwareprüfung. Die nachstehenden Funktionsabschnitte dienen als WPF-Funktionsreferenz; sie behaupten keine bereits nachgewiesene Gleichheit aller Avalonia-Pfade.

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
  LIS2.WindowsMedia/
  LIS2.WmpLegacy/
  LIS2.Runtime/
  LIS2.App.Avalonia/
  LIS2.App/

tests/
  LIS2.Core.Tests/
  LIS2.Display.Tests/
  LIS2.Sources.Tests/
  LIS2.Fans.Tests/
  LIS2.Winamp.Tests/
  LIS2.WindowsMedia.Tests/
  LIS2.WmpLegacy.Tests/
  LIS2.App.Tests/
  LIS2.App.Avalonia.Tests/

tools/
  LIS2.ProtocolTester/
  LIS2.WinampSimulator/
  LIS2.WmpLegacySimulator/

native/
  LIS2.WinampPlugin/
  LIS2.WmpLegacyPlugin/
  LIS2.WmpLegacyVisualization/
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

Die Anwendungen laufen in der interaktiven Benutzersitzung. Avalonia implementiert Windows-Tray, Schließen/Minimieren ins Tray und Windows-Run-Autostart. Linux-Tray/Autostart, Single Instance und Splash sind im Avalonia-Pfad offen. Die tatsächliche Aufteilung von Runtime, UI und Plattformadaptern steht in [architecture.md](architecture.md).

Avalonia speichert den Workspace als `avalonia-workspace.json` im lokalen Anwendungsdatenverzeichnis. Transport/Port und HA-Aktivierungsflag fehlen darin. Helligkeit wird gespeichert, aber noch nicht mit dem Livepfad verbunden. Tokenstore und dessen offenen Linux-Ausweichpfad beschreibt [home-assistant-integration.md](home-assistant-integration.md).

Deliberate UX decisions:

- no first-run wizard
- Kein allgemeiner Einstellungsimport/-export. Ein Windows-Migrationsimport ist im Code vorhanden, sein UI-Zugang liegt in einem ausgeblendeten Bereich.
- no restore-defaults workflow

## Quality / CI

Der Avalonia-Workflow prüft Core-, Display-, Fans- und Avalonia-Tests auf Windows/Ubuntu und publiziert Windows x64 sowie Linux x64/ARM64. [CI 39435226116](https://github.com/Eitschman/lis2-control-center/actions/runs/39435226116) ist für die genannte Codebasis mit sieben erfolgreichen Jobs nachgewiesen. Dies umfasst Headless-Prüfungen und Paketdateiprüfungen, keinen Avalonia-GUI-Start oder echten ARM64-Lauf.

Der getrennte Windows-Workflow baut/testet die Solution und führt WPF-Smoke-/UI-Regression aus. Getaggte Releases veröffentlichen weiterhin WPF. Auslöser und Prüflücken stehen in [release.md](release.md).

## Remaining scope

Die priorisierte Restarbeit steht in [TODO.md](../TODO.md): Seiten-/Quellenintegration, Einstellungen und gewählter Transport, Protokolllog/GUI-Smoke, Medien-/Tokenstore-Grenzen, WPF-Parität und Zielsystemprüfung. WMP Legacy wird in Avalonia unter Windows aktuell gestartet, obwohl der Produktumfang zurückgestellt ist; dieser Widerspruch ist offen.

Physische Geräteprüfung wird getrennt ausgewiesen und blockiert keine rein softwareseitigen Pakete. Bytegenaue Protokollunsicherheiten bleiben in [protocol.md](protocol.md) und [reverse-engineering.md](reverse-engineering.md).
