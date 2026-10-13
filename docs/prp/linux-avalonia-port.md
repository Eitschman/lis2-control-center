# PRP: Cross-platform LIS2 Control Center (Avalonia / Linux)

Status: **PLANNED — NOT STARTED**. Baseline: repository main as inspected 2026-10-13. This document is a future implementation checklist, not evidence of completed porting. Do not begin implementation without a separate decision. Preserve existing Windows release behavior throughout.

## Goal and scope
Deliver a native Linux desktop build (first x64, then ARM64), retaining the current LIS2 display/page/fan features and Home Assistant integration. Continue supporting Windows Winamp (native x86 plugin) and modern Windows Media Player (GSMTC). On Linux add MPRIS-compatible players (e.g. VLC/Plexamp when MPRIS is exposed); Wine Winamp interoperability is optional research, not a guaranteed deliverable. Windows Media Player is not a native Linux application. Use **Avalonia UI** as preferred UI migration path; Wails/Go was assessed and rejected provisionally because it introduces a Go/C# boundary or backend rewrite.

## Baseline and references
- `README.md`: features, known protocol gaps, release status.
- `docs/architecture.md`: serialized hardware queue, fail-safe priority, provider boundaries.
- `docs/protocol.md`, `docs/reverse-engineering.md`: hardware/protocol constraints.
- `docs/winamp-integration.md`, `docs/home-assistant-integration.md`, `docs/release.md`, `TODO.md`.
- `LIS2ControlCenter.sln`: Core, Display, Sources, Fans, Winamp, WindowsMedia, WmpLegacy, App, tools and tests.
- `src/LIS2.App/LIS2.App.csproj`: WPF + WinForms, ProtectedData, PowerShell icon generation, Windows-only UI.
- `src/LIS2.App/MainWindow.xaml`, `MainWindow.xaml.cs`: WPF view and mixed orchestration; directly instantiates source registry, scheduler, fan controller, display runtime, data sources, dispatcher timers, transport and tray.
- `src/LIS2.App/App.xaml.cs`: WPF startup, splash, Windows theme events.
- `src/LIS2.Core/SerialLis2Transport.cs`: System.IO.Ports, 19200/8N1, serial port lifecycle.
- `src/LIS2.Winamp/WinampPipeServer.cs`: newline-delimited JSON snapshots via NamedPipeServerStream.
- `src/LIS2.Sources/LIS2.Sources.csproj`: LibreHardwareMonitorLib 0.9.6.
- `src/LIS2.WindowsMedia`: Windows GSMTC source.
- `native/LIS2.WinampPlugin`: native Win32 gen_lis2.dll.
- `.github/workflows/build.yml`: Windows restore/build/tests/WPF UI smoke and regression, win-x64 publish and x86 Winamp plugin; deferred WMP Legacy jobs.

**Evidence limits:** Above paths and key entry points were inspected, but not every file, runtime behavior or dependency has been audited. Before coding, enumerate repository tree and all C# / XAML / native sources; validate every candidate file and method name. No Linux build or hardware test has yet been performed.

## Definition of done
- Windows CI and existing Winamp/GSMTC features remain green throughout.
- Linux x64 desktop starts and runs on a real Linux desktop; ARM64 validated separately.
- Serial LIS2 or virtual transport, brightness, glyphs, page templates, scroll/events, HA and four fan modes operate as applicable.
- Linux MPRIS metadata, playback state and timeline available to page templates; missing player state handled.
- Linux hardware sensors have stable identities and safe missing-sensor behavior; fan fail-safe verified under disconnect and invalid data.
- UI pages, editors, localization, theme, tray, autostart, logs and settings parity documented; differences explicitly tracked.
- Linux artifacts, dependencies, install instructions, checksums, smoke tests and CI implemented.
- No regression of safety-sensitive serial command ordering.

## Phase 0 — Freeze and inventory (all unchecked)
- [ ] Record baseline main commit SHA, latest passing Windows workflow and release artifacts.
- [ ] Inventory complete repo tree, csproj properties, package/runtime compatibility and every Windows-only API. Initial verified source reconnaissance: [linux-port-inventory.md](linux-port-inventory.md); exhaustive recursive audit still open.
- [ ] Review `AGENTS.md` / `CLAUDE.md` if present; list all WPF/WinForms references and code-behind dependencies.
- [ ] Map each existing tab, dialog, custom control, page designer, CG builder, virtual preview, localization and settings interaction. Initial UI entry points identified; full census pending.
- [ ] Map each `DispatcherTimer` and startup/shutdown event; identify concurrency assumptions.
- [ ] Map all Windows-specific settings, DPAPI, filesystem paths, registry, startup, tray, system theme, icon and logging code.
- [ ] Enumerate tests and coverage gaps, hardware validation limitations and reproducible virtual-LIS2 scenarios.
- [ ] Reconfirm current Avalonia/.NET version support and licensing from official documentation before selecting versions.
- [ ] Document actual feature parity matrix (Windows baseline vs Linux target).

**Critical discovery:** `Directory.Build.props` globally targets `net8.0-windows10.0.19041.0`; portable libraries must be retargeted explicitly. `MainWindow.xaml.cs` currently has 4551 lines. `LIS2.ProtocolTester` is also WPF. See [verified inventory](linux-port-inventory.md).

## Phase 1 — Decouple runtime from WPF
- [ ] Add a UI-independent `src/LIS2.Runtime` project (name provisional), with lifecycle/start/stop, app orchestration and dependency injection.
- [ ] Move scheduler, display update runner, event queue, source lifecycle and timers out of `MainWindow.xaml.cs`.
- [ ] Move fan regulation loops out of WPF dispatcher; use cancellation-aware async services and enforce safe shutdown.
- [ ] Extract application state/view models and commands; UI must only bind/invoke services.
- [ ] Introduce platform service contracts for media, hardware sensors, serial-port discovery, tray, autostart, secrets and OS appearance.
- [ ] Preserve existing template variable names and settings compatibility; write migration tests.
- [ ] Keep WPF as Windows adapter until Avalonia parity is proven.
- [ ] Add headless runtime tests with virtual transport and deterministic clocks/timers.

## Phase 2 — Shared libraries and Linux providers
- [ ] Verify Linux compilation/test execution for `LIS2.Core`, `LIS2.Display`, `LIS2.Fans` independently.
- [ ] Validate `SerialLis2Transport` on Linux using `/dev/serial/by-id/*` preferred over unstable tty numbering; verify permissions (udev/groups), unplug/replug, timeouts and reconnection.
- [ ] Keep all hardware commands serialized; after reconnect restore **safe fans first**, then brightness, glyphs, frame, normal fan regulation.
- [ ] Split LibreHardwareMonitor Windows implementation behind sensor interface; implement Linux backend using established interfaces/libraries (hwmon/sysfs/libsensors as validated), not shell polling by default.
- [ ] Define stable sensor keys and migration/alias mapping; preserve configured-but-unavailable sensors and fan fail-safe.
- [ ] Confirm HA WebSocket client works on Linux; test reconnect, attributes, aliases, token handling and no regression to read-only scope.
- [ ] Provide OS-appropriate secret storage (Linux Secret Service/libsecret where available, explicit secure fallback policy); never silently write existing secrets unencrypted.
- [ ] Implement cross-platform app data/config/log locations, backups/upgrade migration, file locking and graceful errors.
- [ ] Test virtual LIS2 frame/glyph/brightness/fan state and protocol output byte-for-byte against baseline.

## Phase 3 — Media integrations
- [ ] Retain native Windows Winamp x86 plugin, pipe protocol and snapshot parser; test Windows behavior unchanged.
- [ ] Retain Windows 11 GSMTC provider; keep WMP Legacy explicitly deferred.
- [ ] Implement Linux MPRIS2 D-Bus provider: discovery, multiple-player selection, metadata, status, position, disconnect and reconnect.
- [ ] Define generic media snapshot/interface without breaking `Winamp.*` and `WindowsMedia.*` existing template variables.
- [ ] Define Linux-facing variables (e.g. `Media.*`) and presets; specify availability and compatibility in docs.
- [ ] Test VLC and Plexamp individually; MPRIS support must be confirmed in each actual player/version.
- [ ] Treat Winamp running under Wine as an optional feasibility spike: investigate Wine named-pipe interoperability, 32-bit plugin loading and JSON transport. No assumption of native Windows Media Player on Linux.
- [ ] Document spectrum/VU limitation: MPRIS metadata does **not** expose Winamp's raw 20-band spectrum; do not claim parity without a separate audio-analysis backend.

## Phase 4 — Avalonia UI
- [ ] Create `src/LIS2.App.Avalonia` as separate app initially; do not replace `LIS2.App` WPF.
- [ ] Port shell/navigation, dashboard, device connection and virtual LIS2 display first.
- [ ] Port page sequence editor, template/visibility builders, preview, scrolling and overlays.
- [ ] Port CG Builder, custom glyph editing and raw ROM test UI.
- [ ] Port hardware sensor browser, fan channels, curve editor and fail-safe status.
- [ ] Port Home Assistant entity/attribute browser and source configuration.
- [ ] Port Winamp, WindowsMedia and Linux media cards with capability-based visibility.
- [ ] Port all settings, diagnostics, export, help, six-language localization and theme modes.
- [ ] Implement Linux tray behavior for KDE/other desktops, window close-to-tray, single instance, autostart and splash; document Wayland/X11 differences.
- [ ] Replace WPF-specific converters, bindings, resource dictionaries, window chrome, icon generation and WinForms tray with Avalonia/platform equivalents.
- [ ] Compare each screen with Windows baseline and test high-DPI, keyboard navigation, resizing, localization overflow and startup failure behavior.

## Phase 5 — CI, packaging and validation
- [ ] Preserve existing Windows Actions and native plugin artifacts unchanged while introducing Linux jobs.
- [ ] Add Linux x64 restore/build/unit tests, virtual transport smoke tests and UI smoke tests with suitable display environment.
- [ ] Add ARM64 build and actual ARM64 runtime validation (not just cross-publish).
- [ ] Publish self-contained Linux x64/ARM64 artifacts; decide tar.gz/AppImage/deb only after dependency testing.
- [ ] Document GTK/Wayland/X11/DBus/runtime dependencies, serial device access and secure secret-store prerequisites.
- [ ] Add checksums, version metadata and release documentation; verify extracted archive launch.
- [ ] Test on representative Debian/KDE desktop and Raspberry Pi 5 ARM64; record exact distro/kernel/runtime versions.
- [ ] Test real LIS2 hardware only with explicit controlled fan-safe procedure; keep virtual transport as default during development.
- [ ] Verify Windows win-x64, WPF UI smoke/regression, native gen_lis2.dll and release ZIP remain unchanged.
- [ ] Review crash handling, permissions, logs, restart behavior, disconnected sensors, and serial unplug/replug.
- [ ] Update README, architecture, troubleshooting and user guide; decide whether WPF retirement is warranted only after parity.

## Implementation ordering and checkpoints
1. **Gate A:** Inventory and feature matrix approved; baseline Windows CI green.
2. **Gate B:** Headless runtime extracted and tested; Windows application still green.
3. **Gate C:** Linux core + virtual LIS2 + sensors/media adapters pass unit/integration tests.
4. **Gate D:** Avalonia Linux UI reaches feature parity for virtual hardware.
5. **Gate E:** Hardware safety and real-device validation complete; Linux release candidate.
6. **Gate F:** Windows Avalonia parity reviewed; only then consider removing WPF.

## Risks / non-goals
- Fan safety is non-negotiable; never use unknown LIS2 commands or alter fail-safe semantics to obtain UI parity.
- UI parity is not automatic: WPF XAML cannot be copied unchanged into Avalonia.
- Wine/Winamp, WMP Legacy, full spectrum capture on Linux and macOS are **not** prerequisites for first Linux release.
- Do not assume hardware sensor IDs or permissions remain identical across OSes.
- Do not migrate user settings destructively; provide backups and explicit fallback handling.
- No implementation, branching or changes to existing source/build files are authorized by this planning document.

## Status log
- [x] 2026-10-13: Initial architecture evaluation; Avalonia selected as preferred direction over Wails.
- [x] 2026-10-13: Planning checklist recorded.
- [ ] Implementation started.
- [ ] Linux CI build green.
- [ ] Linux UI feature parity reached.
- [ ] Physical Linux LIS2 hardware verified.
