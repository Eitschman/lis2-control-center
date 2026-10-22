# LIS2 Control Center

Control software for the **VL System L.I.S. 2**, featuring a 20x2 VFD and four fan channels.

> **Status:** Shared Avalonia application for Windows and Linux<br>
> **Target platforms:** Windows x64, Linux x64 and Linux ARM64<br>
> **Implementation:** C# / .NET 10 LTS, Avalonia 11.3.22; native Win32/x86 Winamp plug-in<br>
> **License:** MIT

LIS2 Control Center is an independent replacement for the obsolete VL System Multimedia Control Center (MCC). It combines direct LIS2 hardware control with programmable display pages, PC hardware monitoring, Winamp and Windows 11 Media Player telemetry, Home Assistant data and fan control.

> **Preserve the hardware. Replace the limitations of its software.**

## Download and installation

[Release v0.1.2](https://github.com/Eitschman/lis2-control-center/releases/tag/v0.1.2) includes the following packages:

| System | Download |
| --- | --- |
| Windows x64 | [Portable ZIP](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.2/LIS2-Control-Center-0.1.2-win-x64.zip) |
| Debian/Ubuntu x64 | [Debian package for amd64](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.2/lis2-control-center_0.1.2_amd64.deb) |
| Debian/Ubuntu ARM64, Raspberry Pi 5 | [Debian package for arm64](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.2/lis2-control-center_0.1.2_arm64.deb) |
| Linux x64 | [Portable archive](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.2/LIS2-Control-Center-0.1.2-linux-x64.tar.gz) |
| Linux ARM64 | [Portable archive](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.2/LIS2-Control-Center-0.1.2-linux-arm64.tar.gz) |

On Windows, extract the ZIP and run `LIS2.App.Avalonia.exe`. For portable Linux packages, extract the archive and run `./LIS2.App.Avalonia`. The .NET 10 runtime is included; no separate .NET installation is required.

On Debian/Ubuntu, run these commands from the download directory:

```sh
sudo apt install ./lis2-control-center_0.1.2_amd64.deb
lis2-control-center
```

On ARM64, use `lis2-control-center_0.1.2_arm64.deb` instead. The application also appears in the application menu. Configure serial port permissions when using physical LIS2 hardware.

Linux Home Assistant token storage normally uses `secret-tool` and an unlocked desktop keyring; local AES storage is available only after explicitly selecting it in the application. A keyring is not required for features that do not use Home Assistant.

Linux media integration requires `playerctl`. The Debian packages recommend `libsecret-tools` and `playerctl`; see the [packaging guide](docs/release.md#debian-pakete).

Also available: the [Windows Protocol Tester](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.2/LIS2-ProtocolTester-0.1.2-win-x64.zip), [Winamp plug-in gen_lis2.dll](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.2/gen_lis2.dll) and [SHA-256 checksums](https://github.com/Eitschman/lis2-control-center/releases/download/v0.1.2/SHA256SUMS.txt). Place the Winamp plug-in in the `Plugins` directory of 32-bit Winamp. The [release notes](docs/release-notes.md) describe the release scope and outstanding manual checks. Application and Protocol Tester packages include license texts, third-party notices and source references.

## Project status

As of October 22, 2026, PORT-08 includes verified archive launches on Windows x64, Linux x64 and native Linux ARM64, plus Debian packages for amd64/arm64. The shared application in `src/LIS2.App.Avalonia` provides ten main views, display/page/event runtime, custom glyphs, Windows/Linux sensors, fan control with explicit authorization for serial output, Home Assistant, media sources, workspace persistence, help and About. The modal variable browser is an accepted difference from WPF.

CI passes 459 Avalonia tests on Windows and 460 on Linux; the Linux keyring test is skipped on Windows. These include eight Protocol Tester UI regressions. Device controls, settings and all ten main views are tested using actual Avalonia controls and Skia rendering. Static text and dynamic messages follow the selected language; metadata and technical keys remain unchanged. Help supports German, English, French, Turkish, Russian or the system language, including changes while the dialog is open. Native Windows/Linux smoke tests cover the main application and Protocol Tester. See the [status report](docs/prp/linux-port-status.md) for CI evidence and visual checks. Extracted packages and native ARM64 execution pass in CI; physical LIS2 validation and further manual desktop checks remain open. The user has confirmed Debian package operation on a Raspberry Pi 5 with Debian 13, KDE Plasma 6.3.6 and a Wayland session, including release v0.1.0 with .NET 10.

The [Avalonia PRP](docs/prp/linux-avalonia-port.md) defines the requirements, the [status report](docs/prp/linux-port-status.md) records the verified implementation, and the [parity audit](docs/prp/wpf-avalonia-parity-audit.md) describes differences from WPF. [TODO.md](TODO.md) lists the remaining priorities. Transport, port, Home Assistant enablement, Linux token storage selection and brightness are persisted. Shared display actions use the applied transport. Windows settings import is available; manual fan output respects channel limits and requires authorization for each serial operation. Live Home Assistant and media player checks on target systems remain open. PORT-06A adds the spectrum variable, visible glyph previews, an integrated condition browser, direct sensor selection, consistent presets, save feedback and diagnostic fields. The pixel grid and precise curve points are editable. Release v0.1.2 uses .NET 10 LTS and includes license notices. Further manual desktop checks remain on the list.

## Features

The following features belong to the Avalonia application. The porting status report documents platform-specific media, sensor and desktop services and outstanding target-system checks. The WPF reference remains available at Git commit `922ca1b`; the current source tree contains no WPF application.

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

- Windows sensors through LibreHardwareMonitor
- Linux sensors through `/proc`, `/sys` and hwmon: CPU load/frequency, memory/swap, disk/network throughput, filesystem usage, temperatures and fan speeds, depending on available system data
- searchable/filterable hardware browser grouped by device
- aliases and favorites
- configured-but-missing sensors remain visible as unavailable
- direct assignment of sensors to fan channels
- four fan channels with **Fixed, Curve, Follow, External and Off** modes
- minimum/maximum/fail-safe output
- missing/invalid sensor fail-safe handling
- fan-curve editor/preview, live input/output display and optional hysteresis

### Winamp

The Winamp integration runs on Windows; Linux media players use the MPRIS integration described below.

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

Windows Media Player Legacy remains outside the current product scope. The application does not start the Legacy source or display a Legacy card. Native Legacy CI artifacts are disabled.

### Linux media players

- MPRIS-compatible desktop players queried through `playerctl`
- automatic player selection or a saved selection of a specific player
- title, artist, album, playback state and elapsed/duration values when supplied by the player
- `Media.*` display variables and direct page creation from the Media view
- a selected player remains disconnected while unavailable instead of switching to another player

Install `playerctl` for this integration, for example with `sudo apt install playerctl` on Debian/Ubuntu. It is a recommended dependency of the Debian package. Winamp VU/spectrum telemetry and the Windows GSMTC provider are separate Windows integrations.

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

### Desktop application

- tray support, subject to the tray integration available on the Linux desktop
- autostart through the Windows user registry or a Linux XDG autostart entry
- Light, Dark and system themes
- runtime localization: System, English, German, French, Turkish and Russian with English fallback
- context-sensitive in-app help on every main page, including complete page/template configuration reference, all static display variables, dynamic hardware-variable discovery and detailed Home Assistant examples for states, aliases, attributes, formatting and visibility
- application/version/about information, including the Avalonia version, MIT license and access to the packaged license files
- persistent JSON settings under the user's local application data
- migration of legacy WPF `settings.json` files, plus separate page and custom-glyph file loading/saving
- custom LIS2 application/tray/taskbar icon
- diagnostics/logging
- decoded virtual-LIS2 command history and full virtual state inspection
- exportable diagnostic report
- native Windows title bar follows the selected Light/Dark/System theme
- coalesced asynchronous display rendering to prevent overlapping writes

## Architecture

```text
Clock / Home Assistant           Windows/Linux sensors and media
          |                                    |
          v                                    |
SourceRuntime / DataSourceRegistry              |
          |                                    |
          +------------------+-----------------+
                             |
                             v
                     PageSourceValues
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
- `src/LIS2.App.Avalonia`: shared desktop application and platform adapters for Windows/Linux.
- `src/LIS2.Runtime`: UI-independent runtime, background loops, Linux sensors and platform contracts; see [architecture.md](docs/architecture.md).
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

{Media.Artist}
{Media.Title}
{Media.State}
{Media.Elapsed}
{Media.Duration}

{HA.sensor.example}
{HA.sensor.example.Unit}
{HA.sensor.example.attribute.some_attribute}
```

Aliases provide shorter stable names for hardware/Home Assistant values where configured.

`Winamp.*` and `WindowsMedia.*` refer to the Windows integrations; `Media.*` refers to Linux MPRIS players. Available hardware keys depend on the operating system and detected sensors; use the variable browser to select actual keys.

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

The following commands build the solution, including the Avalonia application, Avalonia protocol tester, libraries and Windows GSMTC provider. Requirements:

- Windows
- .NET 10 SDK; `global.json` selects a stable SDK from the 10.0 series
- CMake/MSVC when building the native Winamp plug-in

```powershell
dotnet restore .\LIS2ControlCenter.sln
dotnet build .\LIS2ControlCenter.sln -c Release
dotnet test .\LIS2ControlCenter.sln -c Release --no-build
```

On Linux, build and test the portable projects directly:

```text
dotnet build src/LIS2.App.Avalonia/LIS2.App.Avalonia.csproj --configuration Release
dotnet test tests/LIS2.App.Avalonia.Tests/LIS2.App.Avalonia.Tests.csproj --configuration Release
```

Local and GitHub verification results are documented in the porting status. Publishing the Windows GSMTC provider requires additional steps.

## CI and development artifacts

`build.yml` validates `main` and pull requests through the reusable `linux-portable.yml` workflow. The same workflow validates the Avalonia branch and runs Windows/Linux tests, native desktop smoke tests, archive launch tests and Debian installation checks. Windows also builds the entire solution and tests the data-source and media libraries. The Windows application is scanned with Microsoft Defender.

Development artifacts:

- `LIS2-Avalonia-win-x64`, `LIS2-Avalonia-linux-x64`, `LIS2-Avalonia-linux-arm64`: self-contained archives with SHA-256 files.
- `LIS2-Debian-linux-x64`, `LIS2-Debian-linux-arm64`: installable Debian packages with checksums.
- `LIS2-ProtocolTester-win-x64`: self-contained Avalonia protocol tester; its interface is tested natively on Windows and Linux.
- `gen_lis2-win32`: native x86 Winamp plug-in.

The Windows application includes the GSMTC provider and its WinRT dependencies. Screenshots and test results are uploaded as separate artifacts. Tagged releases use the same Avalonia packaging workflow with the tag version and publish three portable application archives, two Debian packages, the Windows protocol tester, the Winamp plug-in and `SHA256SUMS.txt`. See the [packaging guide](docs/release.md).

The Debian packages include the .NET runtime, an application menu entry and the application icon. Install with `sudo apt install ./lis2-control-center_<version>_amd64.deb`, or use the `_arm64.deb` package on ARM64. See the [Debian packaging guide](docs/release.md#debian-pakete) for details and installation checks.

## Documentation

- [Avalonia PRP](docs/prp/linux-avalonia-port.md)
- [Current porting status](docs/prp/linux-port-status.md)
- [Project and platform inventory](docs/prp/linux-port-inventory.md)
- [WPF/Avalonia parity audit](docs/prp/wpf-avalonia-parity-audit.md)
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

- no general full-settings import/export; legacy WPF settings migration and separate page/custom-glyph files are supported
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

## License and third-party software

LIS2 Control Center is distributed under the [MIT License](LICENSE). Third-party components retain their own copyrights and licenses.

The application uses the following components, including platform-specific and transitive dependencies:

| Component | Purpose | License information |
| --- | --- | --- |
| Avalonia 11.3.22 and Fluent theme | Desktop interface | MIT |
| .NET 10 and System libraries | Runtime, serial ports and operating-system integration | MIT and bundled third-party notices |
| SkiaSharp / Skia and HarfBuzzSharp / HarfBuzz | Graphics and text rendering | Binding licenses and upstream notices included in the packages |
| Inter font | Interface typography | SIL Open Font License 1.1 |
| LibreHardwareMonitor, BlackSharp.Core, DiskInfoToolkit and RAMSPDToolkit-NDD | Windows hardware monitoring | MPL-2.0 |
| HidSharp | Hardware access used by monitoring dependencies | Apache-2.0 |
| Tmds.DBus.Protocol | Linux D-Bus communication | MIT |
| MicroCom.Runtime | Native interoperability | MIT |
| Mono.Posix.NETStandard | Unix interoperability | Mono license terms |
| ANGLE | Windows graphics support | Bundled upstream license and notices |
| Microsoft Windows SDK / WinRT | Windows media integration | Microsoft SDK terms and bundled notices |

Each application and protocol-tester package includes `LICENSE`, `THIRD-PARTY-NOTICES.txt` and a `licenses/` directory with original license texts, copyright notices and NuGet metadata. `licenses/manifest.json` records exact dependency versions and source references, including source archives for MPL-2.0 components. The inventory may include dependencies for other platforms. See the [license sources](tools/packaging/licenses/sources.json) and [packaging guide](docs/release.md) for the packaging process.
