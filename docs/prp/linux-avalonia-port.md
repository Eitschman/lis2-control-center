# PRP: Cross-platform LIS2 Control Center (Avalonia / Windows + Linux)

Status: **IMPLEMENTATION STARTED ON feature/linux-avalonia** (2026-10-13). Preserve existing Windows release behavior throughout. See [implementation status](linux-port-status.md).

## Binding verification policy — owner clarification 2026-10-14

- **No physical LIS2 device is available.** This was already true during the original WPF development. Do not block implementation packages, automated acceptance, or port completion on physical-device testing; do not claim physical verification. Preserve serialized 19200/8N1 protocol, explicit nonpersistent fan authorization, fail-safe, simulated fault injection and virtual-transport regression tests.
- **No manual desktop or screenshot acceptance until the complete port is implemented.** The owner will perform desktop testing only after the application reaches full functional and UI parity in code and the target platform builds are ready. During implementation, use source-based WPF comparison, screen/control inventories, headless tests, simulated interactions where automatable, and CI; defer actual desktop visual checks and mark them pending rather than failed.
- **Automated technical package acceptance** requires all agreed implementation scope, headless regression tests, current HEAD CI, and updated PRP/status documentation. Missing physical hardware or deferred manual desktop checks are not package blockers. Full product release and WPF retirement still require the owner's final desktop acceptance.
- **Do not repeatedly ask the owner to test unfinished builds.** Continue autonomously through coherent packages, and clearly distinguish automated verification from eventual manual desktop sign-off.

## Active execution protocol — binding update 2026-10-13

This section supersedes the earlier immutable two-commit Phase 1 packaging plan and any obsolete next-work lists. The owner explicitly approved splitting **fan Integration & Safety** into two independently testable, cohesive packages. The strict no-partial-package rule still applies **within** each agreed package. Never reinterpret an unfinished package as done.

### General package-sizing authority — applies to the ENTIRE port

**The assistant independently chooses and sizes the next work package.** The owner must not have to break work into tasks, repeatedly approve boundaries, or signal "green" to proceed. This rule applies to all remaining phases (runtime, sensors, serial, UI, media, settings, packaging, testing and WPF retirement), not just fan work.

- Prefer **large, coherent, end-to-end vertical slices** with a meaningful, demonstrable result, spanning implementation, integration, regression tests and documentation. Avoid file-by-file, method-by-method, placeholder-only or CI-trigger commits.
- Balance scope against **actual executability in the available tools and context window**. A package should be substantial but small enough to inspect, implement, review, commit and validate reliably without relying on future turns to finish its agreed acceptance criteria. Avoid giant packages that are practically impossible to complete.
- Choose boundaries by **functional cohesion, safety and testability**, not arbitrary numbers of files or commits. If a feature cannot safely be enabled yet, a complete headless subsystem with a stable interface, realistic simulation and comprehensive tests may be a valid package; describe its limitations clearly.
- Before starting, briefly record the package's intended outcome, required files/layers, dependencies, acceptance tests and explicit exclusions. This is an **internal execution decision**, not a request for user permission. Adjust the size **before committing** when inspection reveals unexpected dependencies; document why. Ask the owner only for a genuine product/safety decision or material scope change, not for ordinary engineering decomposition.
- Keep incomplete changes uncommitted until a coherent package is ready; do not claim completion merely because a commit exists. CI failures may receive targeted repair commits. When CI is green, continue automatically with the next suitably sized package if work is authorized and execution resources permit.
- Give occasional **substantive progress updates** during long execution, including completed integration areas, remaining risks and verified CI results. Do not replace implementation with repeated planning/status messages.
- At handover, provide exact HEAD/CI, what was actually completed and the next **self-contained, appropriately sized package** so another session can continue without renegotiating granularity.

**Priority:** This general rule supersedes older text that mandates an immutable package breakdown or owner approval for routine resizing. Safety gates, WPF parity, CI verification and the ban on misleading partial completion remain mandatory. Owner-approved specific boundaries (such as current fan package A/B) remain in force until finished or a justified revision is recorded.

### Read first on every new chat or work session
1. Read this PRP completely, then `docs/prp/linux-port-status.md`, `docs/architecture.md`, `docs/protocol.md`, relevant source/tests and current GitHub branch HEAD. Do not rely on old chat summaries as source of truth.
2. Check the **latest HEAD's** Windows and Ubuntu CI jobs and exact failing logs **before** any new implementation. Repair failed CI first. An older green commit never validates the new HEAD.
3. Follow the `prp-workflow` execution skill. Work directly in `Eitschman/lis2-control-center` on `feature/linux-avalonia`. Do not modify `main` or WPF during the port.
4. Inspect all existing interfaces and the original WPF behavior before changing implementation; never invent APIs, sensor names, serial commands or UI behavior.
5. Define package scope and acceptance tests, implement all layers of that package together, preflight the complete diff, then **one atomic package commit**. CI-failure-only repair commits are permitted. Do not push a smaller slice to trigger CI.
6. Report substantive interim progress during longer work **without requiring user confirmation**, and continue until a genuine external blocker or a validated package boundary. Do not ask the owner to say "green" between micro-commits.
7. Keep documentation current, distinguishing **code written**, **CI green**, **desktop verified**, **real hardware verified**. Never infer hardware safety from unit tests.
8. When the execution environment lacks local `dotnet`, use the connected GitHub repository and Actions for source editing and validation; this is not a reason to stop implementation. However, CI is only run after a cohesive package is committed, not as a substitute for package completeness.

### Current checkpoint / immediate next action
- Last confirmed green runtime-core HEAD: `d0dd881ca7a69c4f7b6790d98c4547b24052c400`, GitHub Actions run `38436009321`.
- Fan Phase 1 implementation commit: `5b6156d988dc98bc70b9befb71cb21cf10299450`. Its Actions run `38437023413` **FAILED** on both Avalonia builds; shared-library jobs passed. Ubuntu build log shows `FanRuntime.cs(96,69): CS0136` due to conflicting `raw` variable declarations. **Fix and revalidate before starting Phase 2.** A fix commit does not itself mark Phase 1 green.
- No real fan commands are enabled in Avalonia. `SerialDisplaySession` remains display-only, deliberately.

### Fan Integration & Safety — agreed package A: headless runtime
**Scope:** `src/LIS2.Runtime/FanRuntime.cs`, `FanRuntimeLoop.cs`, headless regression tests and status docs. Portable four-channel fan control using existing `LIS2.Fans.FanController`, sensor snapshot validation, fixed/curve/follow/external/off modes, hysteresis, limits, fail-safe, serializable single-flight ticks, output-on-change, failed-write retry, disconnect/reconfigure reset, explicit disabled-by-default output and simulated sensors/sinks. Do **not** connect physical serial fan output or enable UI in this package.

**Acceptance:** compile/test on both Windows and Ubuntu; test missing, null, malformed, nonfinite sensors, curve/hysteresis, all modes, bounds/stop permissions, four-channel output, failed writes, concurrent ticks, disable/re-enable, configuration isolation and shutdown. CI green is required to mark package A complete; physical validation does not apply to simulated output. Review uncovered cases before closing it.

### Fan Integration & Safety — agreed package B: physical integration + Avalonia UI
**Prerequisite:** package A green on current HEAD.
**Scope:** actual platform sensor providers and stable keys/aliases, safe sensor snapshot freshness, fan settings schema/import/persistence compatible with WPF, serial output adapter via existing `LIS2.Core.Lis2Device.SetFansAsync` with serialized commands, explicit hardware safety authorization, safe initial fan state on connect/reconnect **before normal page/brightness writes**, automatic regulation lifecycle/stop/drain/error reporting, manual output limits, and WPF-faithful Avalonia fan channels/curves/live values/status/settings. Do not enable controls or hardware writes until the complete safety path exists.

**Safety acceptance:** missing/stale sensors force configured fail-safe, invalid values never turn off fans, failed writes are retried or cause safe disable/reporting, no concurrent fan/serial command races, no automatic reconnect that skips safe initialization, shutdown drains writes before transport disposal. Validate headless protocol bytes, virtual transport behavior, Linux/Windows builds, UI behavior and real-device procedure separately. If safe reconnect/transport arbitration cannot be demonstrated, **do not enable physical output** and keep package B open. No invented hardware test claims.

### Remaining ordered work after fan package B
- Shared Windows/Linux sensor and media providers (Winamp/GSMTC preserved on Windows; MPRIS2 on Linux); settings/secrets migration.
- WPF-exact Avalonia screen-by-screen functional, visual, interaction, localization and accessibility parity; current sidebar is only scaffolding.
- Windows x64 and Linux x64/ARM64 packaging, desktop smoke tests, Raspberry Pi 5 validation, physical LIS2 controlled safety tests.
- **Mandatory** removal of WPF project/release jobs **only after** verified Avalonia Windows/Linux parity and hardware safety. WPF is read-only reference until then.

### Handover / progress format
Record after each coherent package: branch and exact HEAD SHA, CI run URL and per-job conclusions, affected files, verified acceptance criteria, unverified hardware/UI items, open risks and the **single next actionable task**. Update the status file in the same package commit when possible. Never claim green while the current HEAD is red or pending.

---

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

**Mandatory within each current owner-approved package; see the newer Active execution protocol above.** This applies in every chat and after handovers. The previous repeated violation (pushing incomplete partial implementations despite promising two complete commits) must not recur.

### Historical two-package agreement (superseded for fan work by Active execution protocol above)
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

## Preview artifact workflow (2026-10-14)
- `.github/workflows/linux-portable.yml` now publishes gated downloadable self-contained Avalonia preview packages for `linux-arm64`, `linux-x64` and `win-x64` using `dotnet publish`, and uploads them with a 14-day retention. This is a preview build distribution facility, not a release or final platform acceptance. See `linux-port-status.md` for CI status.

- Follow-up preview packaging hardening: verify executable existence and nontrivial size in each runtime-specific publish directory before uploading. Cross-architecture execution and final desktop acceptance remain pending.

## Pi 5 first-launch UI feedback (2026-10-14)
- Real Pi 5 screenshot revealed window minimum height and sidebar legibility/layout defects. Addressed window sizing, sidebar scrolling/footer separation and explicit high-contrast text in the Avalonia shell. This is an exception to deferred desktop testing because the owner voluntarily supplied a first-launch screenshot; do not interpret it as final desktop acceptance. New-build validation pending.

## Preview artifact download warning (2026-10-14)
- Chrome classified the automatically wrapped GitHub Actions linux-arm64 artifact ZIP as dangerous. CI now packages a conventional self-contained, untrimmed multi-file application rather than a self-extracting single-file binary and provides SHA256 checksums. The outer Actions ZIP remains platform-generated; Chrome acceptance is not guaranteed and requires a new user download attempt. No release/tag, main branch changes, or security bypass. See linux-port-status.md for CI outcome.

## Dashboard transport parity package (2026-10-14)
- Dashboard can now connect/disconnect the existing virtual LIS2 session directly; synchronized enabled states and explicit hardware-free disconnected guidance reduce the dead-end visible on the Pi 5 first-launch screenshot. Automated presentation tests added; CI and final UI acceptance pending. Does not enable physical fan outputs.

## WPF is the sole functional/UI reference (2026-10-14, reaffirmed)
- Do not invent Avalonia controls, dashboard shortcuts or user workflows. Map each user-facing control and handler to src/LIS2.App/MainWindow.xaml and MainWindow.xaml.cs first. The temporary dashboard virtual Connect/Disconnect controls from 50665f7 were confirmed absent in WPF and reverted. Platform-specific implementation may differ internally but must preserve user-facing WPF behavior and layout. Any parity exception requires explicit owner approval.

## Binding WPF UI parity inventory (2026-10-14)
- Source-based overview of **all ten WPF menu tabs** and Avalonia gaps: `docs/prp/wpf-avalonia-parity-audit.md`. The WPF XAML and code-behind are the binding target; no invented features or dashboard controls. Every tab needs detailed control/handler parity mapping and tests before completion. The erroneous Dashboard connection actions from 50665f7 were reverted in 2cb3141.

- Detailed XAML-level WPF control/event inventory now appended to `wpf-avalonia-parity-audit.md` for all ten menus; identifier differences are explicitly not treated as functional gaps. Semantic control/handler parity still open.

## Source-level all-menu functional traceability (2026-10-14)
- `docs/prp/wpf-avalonia-parity-audit.md` now traces every named WPF control (126) and XAML-declared tab event handler (71 tab-scoped occurrences) to Avalonia UI/code evidence, or explicitly records no reliable mapping. Each handler row records actual calls found in WPF code-behind. WPF remains authoritative. UI-name evidence is not functional equivalence. Full dynamic/background/implicit semantic audit and automated parity tests remain open; no tab marked complete solely on this traceability.

## WPF per-menu ownership is mandatory (2026-10-14)

Owner explicitly requires WPF-equivalent location and behavior for every menu, no extra or relocated functionality. Detailed HA/Pages/Settings ownership and existing Avalonia violation documented in docs/prp/wpf-avalonia-parity-audit.md. HA connection and entity management must be under a WPF-equivalent HomeAssistantView, not Pages. This update is documentation, not an implementation fix.

## WPF layout ownership audit for all ten menus (2026-10-14)
- Verified WPF card/section ownership against Avalonia XAML for Dashboard, Display, Pages, Media, Events, Hardware, Fan Control, Settings, Diagnostics and Home Assistant. Per-tab grouping matrix and required corrections in docs/prp/wpf-avalonia-parity-audit.md. Do not implement new features until incorrect placements are corrected or explicitly tracked. Source-level review only; no UI acceptance.

## Initial placement correction (2026-10-14)
- HA URL/token/test/connect and entity picker moved out of PagesView into HomeAssistantView, with WPF Connection / Home Assistant entities card ownership. This is NOT full WPF HA parity: entity details, aliases, favorites, attribute browser and correct enable/save semantics still require implementation. Existing backend controls retained to avoid breaking handlers; no new functions introduced.

## HA entity management WPF correction (2026-10-14)
- Entity/domain search, selected entity, alias/favorite persistence, clipboard key actions, attribute selection and create-page actions wired inside HomeAssistantView; invented HA insert-into-line buttons removed. WPF remains authoritative; compare attribute template-key syntax and actual data semantics in subsequent parity tests. No physical hardware changes.
