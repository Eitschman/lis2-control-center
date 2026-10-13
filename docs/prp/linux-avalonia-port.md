# PRP: Cross-platform LIS2 Control Center (Avalonia / Windows + Linux)

Status: **IMPLEMENTATION STARTED ON feature/linux-avalonia** (2026-10-13). Preserve existing Windows release behavior throughout. See [implementation status](linux-port-status.md).

## Goal and scope
Deliver ONE shared Avalonia desktop application for Windows x64 and Linux x64/ARM64, retaining the current LIS2 display/page/fan features and Home Assistant integration. Continue supporting Windows Winamp (native x86 plugin) and modern Windows Media Player (GSMTC). On Linux add MPRIS-compatible players (e.g. VLC/Plexamp when MPRIS is exposed); Wine Winamp interoperability is optional research, not a guaranteed deliverable. Windows Media Player is not a native Linux application. Use **Avalonia UI** as preferred UI migration path; Wails/Go was assessed and rejected provisionally because it introduces a Go/C# boundary or backend rewrite.

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

**Evidence limits:** Uploaded repository ZIP was fully enumerated and statically scanned (172 files); full semantic review, runtime tests, and real hardware validation are outstanding. See [inventory](linux-port-inventory.md).

## Binding UI parity requirement (confirmed by project owner, 2026-10-13)
The existing Windows WPF application is the **authoritative UI/UX specification**, not merely a feature checklist. The shared Avalonia app must preserve the same screen structure, tab order, control placement, dialog flows, terminology, keyboard/mouse interactions, resizing behavior, visual preview and theme behavior on BOTH Windows and Linux. Reuse the existing WPF layout and resources as the primary migration reference; do not invent a redesigned dashboard or reorganize controls for convenience. The current Avalonia sidebar and virtual-device buttons are temporary engineering scaffolding and are **not** approved as the final layout. Where WPF-specific widgets have no exact Avalonia equivalent, implement the closest behavior and document the discrepancy in a per-screen parity matrix. Compare real rendered screenshots at matching resolution and DPI, plus interaction tests, before marking any screen complete. OS-specific capabilities may differ internally, but they must not arbitrarily change the UI arrangement. Keep WPF intact until Windows Avalonia parity is proven.

## Definition of done
- One shared Avalonia UI and shared runtime ship on Windows x64 AND Linux x64/ARM64; existing Winamp/GSMTC features remain available on Windows. Windows CI stays green throughout.
- Avalonia desktop starts and runs on real Windows x64 and Linux x64 desktops; Linux ARM64 validated separately.
- Serial LIS2 or virtual transport, brightness, glyphs, page templates, scroll/events, HA and four fan modes operate as applicable.
- Linux MPRIS metadata, playback state and timeline available to page templates; missing player state handled.
- Linux hardware sensors have stable identities and safe missing-sensor behavior; fan fail-safe verified under disconnect and invalid data.
- UI pages, editors, localization, theme, tray, autostart, logs and settings parity documented; differences explicitly tracked. **Visual/layout/interaction parity against existing WPF is a hard acceptance criterion on both platforms.**
- Linux artifacts, dependencies, install instructions, checksums, smoke tests and CI implemented.
- No regression of safety-sensitive serial command ordering.

## Phase 0 — Freeze and inventory (all unchecked)
- [ ] Record source baseline commit SHA, latest passing Windows workflow and release artifacts (uploaded ZIP has no .git metadata).
- [x] Inventory complete uploaded repository tree (172 files), all 19 csproj references/packages, global target framework and static Windows-API scan. See [linux-port-inventory.md](linux-port-inventory.md). Full semantic API review and Linux compilation remain open.
- [x] Check archive for AGENTS.md / CLAUDE.md (neither present); scan WPF/WinForms references and code-behind files.
- [x] Inventory ten navigation sections, About/Help windows, WPF resource/theme files, UI test matrix and page/glyph/settings feature groups. Detailed handler-to-service call graph remains open.
- [ ] Map each `DispatcherTimer` and startup/shutdown event; identify concurrency assumptions.
- [x] Locate Windows-specific settings, DPAPI, filesystem paths, registry, startup, tray, system theme, icon generation and diagnostic code by static source scan; behavior/edge cases remain to be tested.
- [x] Enumerate all eight test projects and their test source files; identify virtual-LIS2 tests and outstanding hardware validation in TODO. Actual test execution and coverage measurement remain open.
- [ ] Reconfirm current Avalonia/.NET version support and licensing from official documentation before selecting versions.
- [ ] Document actual feature parity matrix (Windows baseline vs Windows Avalonia vs Linux Avalonia), including visual and interaction differences for every screen.

**Archive-wide static inventory complete (2026-10-13):** 172 files, 113 C# source files, 19 .NET projects, eight test projects. Static scan is not a Linux build or complete semantic code review. **Critical discovery:** `Directory.Build.props` globally targets `net8.0-windows10.0.19041.0`; portable libraries must be retargeted explicitly. `MainWindow.xaml.cs` currently has 4551 lines. `LIS2.ProtocolTester` is also WPF. See [verified inventory](linux-port-inventory.md).

## Phase 1 — Extract WPF behavior into Avalonia and portable runtime
- [ ] Add a UI-independent `src/LIS2.Runtime` project (name provisional), with lifecycle/start/stop, app orchestration and dependency injection.
- [ ] Move scheduler, display update runner, event queue, source lifecycle and timers out of `MainWindow.xaml.cs`.
- [ ] Move fan regulation loops out of WPF dispatcher; use cancellation-aware async services and enforce safe shutdown.
- [ ] Extract application state/view models and commands; UI must only bind/invoke services.
- [ ] Introduce platform service contracts for media, hardware sensors, serial-port discovery, tray, autostart, secrets and OS appearance.
- [ ] Preserve existing template variable names and settings compatibility; write migration tests.
- [ ] Keep WPF unchanged solely as a temporary behavioral/visual reference until Avalonia Windows parity is verified; DO NOT integrate WPF as a runtime adapter.
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
- [ ] Create `src/LIS2.App.Avalonia` as a temporary parallel application; this is the **single future cross-platform Windows/Linux GUI**, not a Linux-only fork. Keep WPF only as migration reference until Windows Avalonia parity is proven.
- [ ] Port shell/navigation, dashboard, device connection and virtual LIS2 display first.
- [ ] Port page sequence editor, template/visibility builders, preview, scrolling and overlays.
- [ ] Port CG Builder, custom glyph editing and raw ROM test UI.
- [ ] Port hardware sensor browser, fan channels, curve editor and fail-safe status.
- [ ] Port Home Assistant entity/attribute browser and source configuration.
- [ ] Port Windows Winamp/GSMTC and Linux MPRIS media cards into the SAME Avalonia app with capability-based visibility.
- [ ] Port all settings, diagnostics, export, help, existing five translation languages plus system-language mode, and theme modes.
- [ ] Implement Linux tray behavior for KDE/other desktops, window close-to-tray, single instance, autostart and splash; document Wayland/X11 differences.
- [ ] Replace WPF-specific converters, bindings, resource dictionaries, window chrome, icon generation and WinForms tray with Avalonia/platform equivalents.
- [ ] Compare each screen with Windows baseline and test high-DPI, keyboard navigation, resizing, localization overflow and startup failure behavior.

## Phase 5 — CI, packaging and validation
- [ ] Preserve existing Windows Actions and native plugin artifacts unchanged while introducing Linux jobs.
- [ ] Add Windows x64 AND Linux x64 Avalonia restore/build/unit tests, virtual transport smoke tests and UI smoke tests with suitable display environments.
- [ ] Add ARM64 build and actual ARM64 runtime validation (not just cross-publish).
- [ ] Publish self-contained Linux x64/ARM64 artifacts; decide tar.gz/AppImage/deb only after dependency testing.
- [ ] Document GTK/Wayland/X11/DBus/runtime dependencies, serial device access and secure secret-store prerequisites.
- [ ] Add checksums, version metadata and release documentation; verify extracted archive launch.
- [ ] Test on representative Debian/KDE desktop and Raspberry Pi 5 ARM64; record exact distro/kernel/runtime versions.
- [ ] Test real LIS2 hardware only with explicit controlled fan-safe procedure; keep virtual transport as default during development.
- [ ] Verify existing Windows WPF build and native gen_lis2.dll remain unchanged during migration; add and validate Windows Avalonia win-x64 packaging and regression tests before switching the release.
- [ ] Review crash handling, permissions, logs, restart behavior, disconnected sensors, and serial unplug/replug.
- [ ] Update README, architecture, troubleshooting and user guide; retire WPF as a REQUIRED end-state after verified Avalonia parity, not an optional decision.

## Non-negotiable product goal: REMOVE WPF (owner directive, 2026-10-13)

- **Final product is ONE Avalonia application for BOTH Windows and Linux (including Linux ARM64). WPF must be removed from the product and release after verified functional, visual, and hardware-safety parity.** WPF is NOT a supported long-term frontend, runtime client, or alternate Windows UI.
- **During migration, existing WPF code is read-only reference material** for exact behavior, screen layout, settings, templates, dynamic scheduling, fan safety, and regression expectations. Keep it buildable temporarily as a regression baseline, but do NOT spend implementation effort wiring WPF to `LIS2.Runtime`, extracting WPF adapters, or maintaining dual frontends as an architectural objective.
- Phase 1 Runtime Core: implement the shared portable orchestration for the **Avalonia target only**; read WPF implementation for parity but integrate only Avalonia. Phase 1 Integration & Safety: fan runtime, Avalonia view models and adapters, headless tests, configuration compatibility. Any earlier checklist demanding WPF runtime integration is superseded by this rule.
- **Removal gate:** after Avalonia parity and Windows/Linux/ARM64 packaging are verified, remove WPF app/project and WPF-only build/release jobs and update docs. Do not delete the reference prematurely before comparison and safe validation. WPF retirement is mandatory, not a future optional decision.
- This section takes precedence over any other text suggesting WPF is a permanent Windows adapter. Do not silently reintroduce that work in future chats.

## Binding implementation workflow: STRICT package/commit gate (owner directive, 2026-10-13)

**Mandatory and higher priority than convenience, context limits, or quick green CI.** This applies in every chat and after handovers. The previous repeated violation (pushing incomplete partial implementations despite promising two complete commits) must not recur.

### Immutable agreed scope for Phase 1 / Package 1
- **Runtime Core (logical commit 1):** finish Phase 1 steps 1–4 and 6 together: standalone portable runtime, display scheduling/event/output orchestration, source lifecycle, timers, concurrency/reconnect/shutdown, and platform contracts. Integration, regression tests, and documentation are part of the same deliverable.
- **Integration & Safety (logical commit 2):** finish steps 5, 7, 8 together: move fan regulation loops without changing safety semantics, integrate Avalonia adapters (NOT WPF adapters), state/view models/commands, headless/compatibility/migration tests and docs.
- Historical commit `1e0dc5d7` is **only an incomplete Runtime Core checkpoint**, not completion of logical commit 1. Do not rewrite published history without permission; finish the remainder in ONE cohesive completion commit. The historical partial commit does not justify further partial pushes.

### NON-NEGOTIABLE PRE-PUSH CHECKLIST (all must pass)
1. **Define package acceptance criteria BEFORE coding**: exact PRP steps, user-visible outcome, affected layers/files, tests, safety constraints, and what constitutes done. Do not silently narrow scope or re-label an intermediate implementation as a complete package.
2. **Inspect all affected existing code and actual interfaces**. Prepare the entire package across runtime, adapters, tests and docs. Verify all automated text replacements actually occurred. Never push speculative references or incomplete stubs presented as finished integration.
3. **NO intermediate Git pushes/commits** for a class, file, partial substep, status note, isolated test, or to trigger CI early. Keep incomplete work in the local working tree or uncommitted staging. If tool/environment limits prevent staging the full package, STOP without committing and clearly report the blocker. Never treat limited turn length as permission to commit a smaller slice.
4. **Preflight**: inspect diff and changed-file inventory; verify each agreed acceptance criterion against implementation, build/test locally where possible, and review regression coverage. If local .NET is unavailable, explicitly record this and use CI only after the full coherent package is ready; do not use repeated partial pushes as a compilation probe.
5. **ONE atomic commit/ref update per COMPLETE logical package**, including all code, tests and docs. If current scope genuinely needs to change, obtain explicit owner agreement **before** changing it or committing. Do not retroactively redefine a package after pushing.
6. **Post-push CI**: check the exact newest HEAD, Windows and Linux results. Fix genuine failures with the minimum necessary repair commit; repair commits are exceptions, not a way to implement missing planned functionality piecemeal. A previous green run is not proof of the current HEAD.
7. **Status honesty**: distinguish planned, implemented-but-unverified, CI-green and hardware-validated. Mark the package complete only when every acceptance criterion is met and latest HEAD CI green. Real hardware tests must be separately identified; fan safety never inferred from CI alone.
8. **Progress reports without Git pushes**: provide substantive updates during long work, and keep going without requesting approval for each substep. Never repeatedly ask for "grün" to unlock micro-commits.

### Enforcement / failure response
- Immediately before **every** create_tree/create_commit/update_ref or equivalent, compare the complete staged file set with the agreed package checklist. If anything is missing, **DO NOT COMMIT**.
- A partially complete package must stay **OPEN** even when all existing CI tests are green.
- If completion cannot be guaranteed in the current session, explain the actual remaining work and preserve a precise handover; do not push an unfinished checkpoint.
- Preserve existing WPF Windows behavior only as an untouched migration reference until Avalonia parity is verified; DO NOT add a WPF runtime adapter. Preserve 1:1 UI parity, fan fail-safe, serialized hardware writes, and the isolated feature branch. Hardware safety cannot be traded for fewer commits.

## Implementation ordering and checkpoints
1. **Gate A:** Inventory and feature matrix approved; baseline Windows CI green.
2. **Gate B:** Headless runtime extracted and tested; Windows application still green.
3. **Gate C:** Linux core + virtual LIS2 + sensors/media adapters pass unit/integration tests.
4. **Gate D:** Avalonia Linux UI reaches feature parity for virtual hardware.
5. **Gate E:** Hardware safety and real-device validation complete; Linux release candidate.
6. **Gate F:** Windows Avalonia feature parity (including Winamp/GSMTC), packaging and real desktop testing verified; only then retire WPF and switch BOTH Windows and Linux releases to shared Avalonia.

## Risks / non-goals
- Fan safety is non-negotiable; never use unknown LIS2 commands or alter fail-safe semantics to obtain UI parity.
- UI parity is not automatic: WPF XAML cannot be copied unchanged into Avalonia.
- Wine/Winamp, WMP Legacy, full spectrum capture on Linux and macOS are **not** prerequisites for first Linux release.
- Do not assume hardware sensor IDs or permissions remain identical across OSes.
- Do not migrate user settings destructively; provide backups and explicit fallback handling.
- Implementation is authorized on the isolated feature branch; changes to main require review/merge.

## Status log
- [x] 2026-10-13: Initial architecture evaluation; Avalonia selected as preferred direction over Wails.
- [x] 2026-10-13: Planning checklist recorded.
- [x] 2026-10-13: Implementation started on feature/linux-avalonia; portable libraries and Ubuntu test workflow committed (not yet verified green).
- [ ] Linux CI build green.
- [ ] Linux UI feature parity reached.
- [ ] Physical Linux LIS2 hardware verified.
