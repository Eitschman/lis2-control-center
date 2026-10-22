# LIS2 Control Center

Steuerungssoftware für das **VL System L.I.S. 2** mit 20x2-VFD und vier Lüfterkanälen.

> **Status:** Gemeinsame Avalonia-Anwendung für Windows und Linux<br>
> **Zielplattformen:** Windows x64, Linux x64 und Linux ARM64<br>
> **Implementierung:** C# / .NET 10 LTS, Avalonia 11.3.6; Winamp-Plug-in als natives Win32/x86-Modul<br>
> **License:** MIT

LIS2 Control Center is an independent replacement for the obsolete VL System Multimedia Control Center (MCC). It combines direct LIS2 hardware control with programmable display pages, PC hardware monitoring, Winamp and Windows 11 Media Player telemetry, Home Assistant data and fan control.

> **Preserve the hardware. Replace the limitations of its software.**

## Download und Installation

[Release v0.1.0](https://github.com/Eitschman/lis2-control-center/releases/tag/v0.1.0) enthält folgende Pakete:

| System | Download |
| --- | --- |
| Windows x64 | [Portables ZIP](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.0/LIS2-Control-Center-0.1.0-win-x64.zip) |
| Debian/Ubuntu x64 | [Debian-Paket amd64](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.0/lis2-control-center_0.1.0_amd64.deb) |
| Debian/Ubuntu ARM64, Raspberry Pi 5 | [Debian-Paket arm64](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.0/lis2-control-center_0.1.0_arm64.deb) |
| Linux x64 | [Portables Archiv](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.0/LIS2-Control-Center-0.1.0-linux-x64.tar.gz) |
| Linux ARM64 | [Portables Archiv](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.0/LIS2-Control-Center-0.1.0-linux-arm64.tar.gz) |

Windows: ZIP entpacken und `LIS2.App.Avalonia.exe` starten. Linux-Archive entpacken und `./LIS2.App.Avalonia` ausführen. Die .NET-10-Laufzeit ist enthalten; eine separate .NET-Installation ist nicht erforderlich.

Debian/Ubuntu, im Downloadverzeichnis:

```sh
sudo apt install ./lis2-control-center_0.1.0_amd64.deb
lis2-control-center
```

Auf ARM64 stattdessen `lis2-control-center_0.1.0_arm64.deb` verwenden. Die App erscheint auch im Anwendungsmenü. Serielle Zugriffsrechte und ein Desktop-Schlüsselbund müssen zum jeweiligen System passen; Hinweise stehen im [Paketierungsleitfaden](docs/release.md#debian-pakete).

Zusätzlich: [Windows-Protokolltester](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.0/LIS2-ProtocolTester-0.1.0-win-x64.zip), [Winamp-Plug-in gen_lis2.dll](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.0/gen_lis2.dll) und [SHA-256-Prüfsummen](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.0/SHA256SUMS.txt). Das Winamp-Plug-in gehört in das `Plugins`-Verzeichnis des 32-Bit-Winamp. Die [Release-Hinweise](docs/release-notes.md) nennen Umfang und offene manuelle Prüfungen. Lizenztexte, Fremdkomponentenhinweise und Quellverweise liegen den App- und Protokolltesterpaketen bei.

## Avalonia-Arbeitsstand

Stand: 22.10.2026, Implementierungsstand PORT-07 mit geprüften Archivstarts für Windows x64, Linux x64 und nativem Linux ARM64 sowie Debian-Paketen auf amd64/arm64. Die gemeinsame App unter `src/LIS2.App.Avalonia` enthält zehn Hauptansichten, Display-/Seiten-/Ereignisruntime, Glyphen, Windows-/Linux-Sensoren, Lüfterregelung mit ausdrücklicher serieller Freigabe, HA, Medien, Workspace, Hilfe und About. Der modale Variablenbrowser ist eine genehmigte Abweichung von WPF.

In CI bestehen 459 Avalonia-Tests unter Windows und 460 unter Linux; der Linux-Schlüsselbundtest ist unter Windows übersprungen. Enthalten sind acht UI-Regressionen des Avalonia-Protokolltesters. Gerätebedienung, Einstellungen und alle zehn Menüseiten werden mit tatsächlichen Avalonia-Controls und Skia-Rendering geprüft. Statische Texte und dynamische Meldungen folgen der gewählten Sprache; Metadaten und technische Schlüssel bleiben unverändert. Die Hilfe folgt Deutsch, Englisch, Französisch, Türkisch, Russisch oder der Systemsprache, auch bei geöffnetem Dialog. Native Windows-/Linux-Smokes prüfen die Hauptanwendung und den Protokolltester. Der aktuelle CI-Nachweis und die Sichtprüfungen stehen im [Status](docs/prp/linux-port-status.md). Extrahierte Paketstarts und native ARM64-Ausführung bestehen in CI; physische LIS2-Prüfung und weiterführende manuelle Desktop-Abnahme bleiben offen. Der Benutzer bestätigt den Debian-Paketstart von Build 622 auf Raspberry Pi 5 mit Debian 13, KDE Plasma 6.3.6 und Wayland-Sitzung.

Die verbindlichen Ziele stehen im [Avalonia-PRP](docs/prp/linux-avalonia-port.md), der aktuelle Befund im [Status](docs/prp/linux-port-status.md), die Unterschiede zur WPF-Oberfläche im [Paritätsaudit](docs/prp/wpf-avalonia-parity-audit.md). [TODO.md](TODO.md) führt die priorisierte Restarbeit. Transport, Port, HA-Aktivierung, lokale Linux-Tokenauswahl und Helligkeit werden gespeichert. Gemeinsame Displayaktionen verwenden den angewendeten Transport. Der Windows-Import ist sichtbar; manuelle Lüfterausgaben beachten Kanalgrenzen und benötigen bei serieller Ausgabe je Aktion eine Freigabe. Live-HA und Liveplayer auf den Zielsystemen bleiben offen. PORT-06A ergänzt die Spektrumvariable, sichtbare Glyphenvorschauen, den angebundenen Bedingungsbrowser, direkte Sensorauswahl, konsistente Presets, Speicherrückmeldungen und Diagnosefelder. Pixelraster und präzise Kurvenpunkte sind bedienbar. Release v0.1.0 verwendet .NET 10 LTS und enthält Lizenzbeilagen. Avalonia-Patchpflege und manuelle Desktop-Abnahme bleiben auf der Liste.

## Funktionsumfang

Die folgenden Funktionen gehören zur Avalonia-Anwendung. Plattformabhängige Medien-, Sensor- und Desktopdienste sowie die noch offenen Zielsystemprüfungen stehen im Portierungsstatus. Die WPF-Referenz ist im Git-Commit `922ca1b` zugänglich; der aktuelle Quellbaum enthält keine WPF-Anwendung.

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
- visibility expressions with string/numeric operators: `=`, `!=`, `>`, `>=`, `<`, `<=`
- visibility builder in the page editor using live source values
- template formatting pipeline with fallbacks, numeric/percent/byte formatting, prefix/suffix and case conversion
- scheduler-driven rotation independent from scrolling animation
- event/overlay queue with priority and lifetime plus UI/test generation
- ready-made presets for Clock, Status, CPU, GPU, Memory, Temperatures, Fans, Winamp Now Playing/VU/Spectrum and Windows Media Player Now Playing

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

### Windows Media Player

The modern Windows 11 Media Player is integrated directly through **Global System Media Transport Controls (GSMTC)**. No player plug-in or additional DLL installation is required.

- automatic detection of the Windows Media Player session
- title, artist, album and album artist
- playback state and timeline/position
- track number/count and playback rate when supplied by Windows
- live session start/change notifications
- `WindowsMedia.*` display variables
- ready-made Now Playing page preset
- dedicated Media-page card alongside Winamp

Windows Media Player Legacy bleibt im Produktumfang zurückgestellt. Die Anwendung startet die Legacy-Quelle nicht und zeigt keine Legacy-Karte. Native Legacy-CI-Artefakte sind deaktiviert.

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
- context-sensitive in-app help on every main page, including complete page/template configuration reference, all static display variables, dynamic hardware-variable discovery and detailed Home Assistant examples for states, aliases, attributes, formatting and visibility
- application/version/about information
- persistent JSON settings under the user's local application data
- custom LIS2 application/tray/taskbar icon
- diagnostics/logging
- decoded virtual-LIS2 command history and full virtual state inspection
- exportable diagnostic report
- native Windows title bar follows the selected Light/Dark/System theme
- coalesced asynchronous display rendering to prevent overlapping writes

## Architecture

```text
Clock / Hardware / Winamp / Windows Media / Home Assistant
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
- `src/LIS2.WindowsMedia` — Windows 11 GSMTC session source and Windows Media display values
- `src/LIS2.App.Avalonia`: gemeinsame Desktop-App und Plattformadapter für Windows/Linux.
- `src/LIS2.Runtime`: UI-unabhängige Runtime, Schleifen, Linux-Sensoren und Plattformverträge; Details in [architecture.md](docs/architecture.md).
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

{WindowsMedia.Artist}
{WindowsMedia.Title}
{WindowsMedia.State}
{WindowsMedia.Elapsed}
{WindowsMedia.Duration}

{HA.sensor.example}
{HA.sensor.example.Unit}
{HA.sensor.example.attribute.some_attribute}
```

Aliases provide shorter stable names for hardware/Home Assistant values where configured.

Template values can be formatted inline:

```text
{HA.sensor.temperature|fallback:--|number:1|suffix:°C}
{HA.sensor.disk_used|fallback:--|percent:0}
{HA.sensor.disk_free_bytes|fallback:--|bytes:1}
{Winamp.Title|fallback:Nothing playing|upper}
```

Supported operations are `fallback`, `number`, `percent`, `bytes`, `prefix`, `suffix`, `upper` and `lower`. Operations can be chained.

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

Die folgenden Befehle bauen die Solution mit Avalonia-App, Avalonia-Protokolltester, Bibliotheken und Windows-GSMTC-Anbieter. Voraussetzungen:

- Windows
- .NET 10 SDK; `global.json` wählt ein stabiles SDK der 10.0-Reihe
- CMake/MSVC when building the native Winamp plug-in

```powershell
dotnet restore .\LIS2ControlCenter.sln
dotnet build .\LIS2ControlCenter.sln -c Release
dotnet test .\LIS2ControlCenter.sln -c Release --no-build
```

Unter Linux werden die portablen Projekte gezielt gebaut und geprüft:

```text
dotnet build src/LIS2.App.Avalonia/LIS2.App.Avalonia.csproj --configuration Release
dotnet test tests/LIS2.App.Avalonia.Tests/LIS2.App.Avalonia.Tests.csproj --configuration Release
```

Die konkreten lokalen und GitHub-Prüfläufe stehen im Portierungsstatus. Für GSMTC enthält das Windows-Publish zusätzliche Schritte.

## CI and development artifacts

`build.yml` prüft `main` und Pull Requests über den wiederverwendbaren Workflow `linux-portable.yml`. Derselbe Workflow prüft den Avalonia-Branch und führt Windows-/Linux-Tests, native Desktop-Smokes, Archivstarts und Debian-Installationsprüfungen aus. Windows baut zusätzlich die gesamte Solution und prüft die Quellen- und Medienbibliotheken. Die Windows-App wird mit Microsoft Defender geprüft.

Entwicklungsartefakte:

- `LIS2-Avalonia-win-x64`, `LIS2-Avalonia-linux-x64`, `LIS2-Avalonia-linux-arm64`: selbstständige Archive mit SHA-256-Dateien.
- `LIS2-Debian-linux-x64`, `LIS2-Debian-linux-arm64`: installierbare Debian-Pakete mit Prüfsummen.
- `LIS2-ProtocolTester-win-x64`: selbstständiger Avalonia-Protokolltester; seine Oberfläche wird unter Windows und Linux nativ geprüft.
- `gen_lis2-win32`: natives x86-Winamp-Plug-in.

Die Windows-App enthält den GSMTC-Anbieter und seine WinRT-Abhängigkeiten. Screenshots und Prüfergebnisse werden als eigene Artefakte hochgeladen. Tag-Releases verwenden denselben Avalonia-Paketbau mit der Tag-Version und veröffentlichen drei portable App-Archive, zwei Debian-Pakete, den Windows-Protokolltester, das Winamp-Plug-in und `SHA256SUMS.txt`. Einzelheiten stehen im [Paketierungsleitfaden](docs/release.md).

Zusätzlich entstehen `LIS2-Debian-linux-x64` und `LIS2-Debian-linux-arm64` mit installierbaren `.deb`-Paketen und SHA-256-Prüfsummen. Sie enthalten die .NET-Laufzeit, einen Startmenüeintrag und das Anwendungssymbol. Installation mit `sudo apt install ./lis2-control-center_<Paketversion>_amd64.deb`, auf ARM64 mit `_arm64.deb`. Einzelheiten und Debian-Prüfungen stehen im [Paketierungsleitfaden](docs/release.md#debian-pakete).

## Documentation

- [Avalonia-PRP](docs/prp/linux-avalonia-port.md)
- [Aktueller Portierungsstatus](docs/prp/linux-port-status.md)
- [Projekt- und Plattforminventar](docs/prp/linux-port-inventory.md)
- [WPF-/Avalonia-Paritätsaudit](docs/prp/wpf-avalonia-parity-audit.md)
- [Project specification](docs/PROJECT_SPEC.md)
- [Architecture](docs/architecture.md)
- [Protocol notes](docs/protocol.md)
- [Reverse-engineering notes](docs/reverse-engineering.md)
- [Winamp integration](docs/winamp-integration.md)
- [Home Assistant integration](docs/home-assistant-integration.md)
- [Release process](docs/release.md)
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
