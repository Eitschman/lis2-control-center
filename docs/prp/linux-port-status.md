# Windows + Linux shared Avalonia port — implementation status

Date: 2026-10-13
Branch: `feature/linux-avalonia`
Status: **Active incremental port; CG Builder and Pages editor available in virtual transport; latest CI must be verified**

## Binding UI acceptance criterion
- User confirmed 2026-10-13: The existing Windows WPF UI is the authoritative reference for **layout, placement, interactions, workflows and visual appearance** on BOTH Windows and Linux. The Avalonia sidebar/virtual buttons are temporary engineering scaffolding, not a proposed redesign.
- Before marking a screen done, compare against the WPF screen at matching resolution/DPI and document any unavoidable Avalonia differences; no arbitrary reorganization or missing controls.

## Target architecture
- ONE Avalonia UI and application runtime for Windows x64 and Linux x64/ARM64.
- Windows-only providers (Winamp, GSMTC, native plugins) and Linux-only providers (MPRIS, Linux sensors) are selected behind common interfaces.
- The WPF app remains an interim regression baseline, not a permanent separate Windows product.
- Both Windows and Linux builds, UI behavior, packaging and hardware safety must be validated before WPF retirement.

## Implemented
- Retargeted `LIS2.Core`, `LIS2.Display`, `LIS2.Fans` to `net8.0`, overriding the inherited Windows-only framework. Existing Windows WPF app remains unchanged.
- Retargeted their three xUnit test projects to `net8.0`.
- Added `.github/workflows/linux-portable.yml` to run these test projects on Ubuntu on branch pushes and PRs.

- Added `src/LIS2.App.Avalonia` with `net8.0`, Avalonia 11.3.6 references, `Program`, `App`, and a static main window displaying ten navigation labels. This is a nonfunctional UI shell, not a port of the existing WPF workflows.
- Expanded GitHub Actions matrix to Windows and Ubuntu for both portable unit tests and Avalonia shell compilation.

- Made all ten sidebar sections selectable in the shared Avalonia shell; selection updates the section heading and explicitly labels unavailable functionality. This is not feature parity.

- Confirmed GitHub Actions run 38406414651: all three portable test projects passed on Windows and Ubuntu; Avalonia builds failed with AVLN3000 due to SelectionChanged AXAML handler. Fixed by subscribing in code-behind (commits d85503f and 15756d6). User subsequently reported green CI; independently verifying the exact newer run remains outstanding.
- Added `VirtualDeviceSession` backed by the existing `VirtualLis2Transport` and `Lis2Device`; connect, disconnect and demo display commands can now be triggered from the shared Avalonia UI. No serial hardware or fan commands are issued. Latest code has not yet passed CI.

- Began WPF-faithful UI port by inspecting the actual `src/LIS2.App/MainWindow.xaml` baseline: restored 250px sidebar, minimum window dimensions, title/help/status header placement, transport/footer areas, dashboard 3:2 preview/connection cards, and virtual VFD text preview. This is a partial structural port only; no claim of full screen parity. Commits ee33220 and 8ca0dd7; new CI pending.

- Ported WPF dashboard layout sections (VFD Preview, System Status, Quick Links, four Fan Outputs in original two-column arrangement, tip panel) into Avalonia. Quick links navigate to matching sections; fan sliders and Apply remain intentionally disabled until safe fan runtime exists. This is UI structure only, not feature parity. Commits 7704688 and 74ddff7; CI pending.

- Display tab first-pass WPF layout port: Quick Display Test (two text lines, send, render both, clear), ROM/special character panel, CG Builder layout, brightness buttons. Quick display operations and brightness now call `Lis2Device` through the existing virtual transport, and dashboard preview reads actual `VirtualLis2State` instead of hard-coded text. ROM and CG Builder controls remain intentionally disabled pending functional port. No physical serial/fan control. Commits 4b3f6ef, 468b31e, 235e766, aff02d2; CI and desktop runtime validation pending.

- Ported original WPF special-character test and ROM 40-byte range navigation (`20`–`D8`) to virtual LIS2. Found and corrected a runtime-only protocol defect: `Lis2Protocol.WriteLine` requires lines **1/2**, whereas the Avalonia virtual demo/quick tests previously used **0/1**. Corrected both demo and quick-test calls; prior green CI only proved compilation, not functional behavior. Commits dcd24d6, 6b9bdb1, 6223ca1, 9ce5773, 6e656c1. CI and interactive tests pending.

- Added new `tests/LIS2.App.Avalonia.Tests` xUnit project with headless regression tests covering demo, 1-based display lines, brightness, clear, raw ROM ranges, invalid input, disconnected writes and special characters. Test project is committed but NOT yet wired into CI and not yet verified. Commits d773df5 and 4d4d92c.

- Added `tests/LIS2.App.Avalonia.Tests` xUnit project and CI job on both Windows and Linux to execute headless virtual-session regression tests (display line indexing, ROM ranges, brightness, clear, connection guard, special characters). Expanded tests for 5x8 glyph validation and eight-row programming. The CG Builder now has editable slot selection and binary row input with a working virtual send operation; saved glyph sets, graphical pixel editor and persistence are still pending. Relevant commits d773df5 through 6996bee. **New CI jobs are not yet confirmed green at this documentation commit.**

- CG Builder functional port expanded: original WPF `CustomGlyphSettings`, `CustomGlyphSetFile` and `BuiltInGlyphSets` copied to the cross-platform Avalonia project (same versioned `.lis2glyphs.json` schema); `GlyphWorkspace` implements eight slots, name/bitmap editing, validated JSON import/export, original examples; Avalonia storage-provider dialogs load/save sets, live 5x8 pixel preview, send-all 64-row virtual protocol operation, and optional virtual-connect auto-programming. Added workspace roundtrip/examples/send-all tests. Note: Save glyph updates in-memory workspace only; persistent app settings, per-pixel mouse grid, named template binding and real-device integration remain pending. Main integrated commit a31f630. CI on latest head pending.

- Pages editor first functional block ported using the exact original WPF `PageDefinition` fields. `PageWorkspace` supports add/duplicate/delete/reorder, JSON roundtrip and `LIS2.Display.TemplateRenderer`-based 20x2 preview. Avalonia Pages view adds editing of name, both templates, duration, refresh, visibility expression, overflow modes, scroll timing and enabled state, plus virtual-device page send. Tests added for CRUD, templates, JSON and invalid imports. Commit 5b7d4b3. Not yet 1:1 complete: condition builder, template-variable picker, event test, persistent settings and full scrolling/scheduler integration remain pending; CI for integrated Pages view pending.
- CG Builder integrated commits a31f630 and a5c85bed were verified successful on Windows and Linux CI; two red runs at intermediate commits 75d82759 and 348042e7 were superseded by green builds.

- Pages editor follow-up: added Windows-style visibility condition builder (free-form template key, six comparison operators, value, apply), live syntax validation via existing `LIS2.Display.VisibilityEvaluator`, cross-platform page JSON import/export and transactional validation of visibility expressions and scroll timings. Existing WPF `PageDefinition` field names preserved. Added visibility semantics/import regression tests. This is manual file import/export, not automatic application-settings persistence; source-variable discovery, running scheduler, and full UI parity remain pending. Earlier intermediate CI red at f47089f and 3404f25 due to nonexistent Avalonia `ComboBox.Text`; corrected by TextBox input in eb c0f1f (commit `ebc0f1f`). Final integrated CI pending.

- Page runtime extraction started: `PagePlayback` is a headless sequence engine using existing `LIS2.Display.PageScheduler`, `TemplateRenderer`, `VisibilityEvaluator`, original page durations, enabled flags and visibility expressions; deterministic `Tick(DateTimeOffset, values)` supports time-based page rotation, immediate visibility changes and no-visible-page result. Added `PagePlaybackTests` for rotation, disabled pages and visibility transitions. Editor save now validates visibility before changing existing page fields. Not yet connected to a running UI timer or real LIS2 device; this is a tested model only pending CI. Commits e92ca95a, 146650a9, c5b4b43c.

- Live virtual page playback integration: Avalonia Pages view now has Start/Stop sequence and active page status, with a guarded 250 ms dispatcher timer calling headless `PagePlayback.Tick`. Writes only when the 20x2 frame changes; validates selected page before starting, stops on disconnect/close/errors, never issues fan commands. Playback uses current in-memory pages and static preview values (no real source-provider updates yet), and changes to page definitions require restart. Commit sequence a6b49a4e, 54a9e1bd, 20f2a94a, 7781fb9f. CI pending; actual GUI behavior not yet manually verified.

- First dynamic page source: `PageClockValues` exposes portable live `Clock.Time`, `Clock.Date`, `Clock.Hour`, `Clock.Minute`, `Clock.Second`, `Clock.Day`, `Clock.Month`, `Clock.Year`, `Clock.DayOfWeek` variables. Page editor preview and virtual page scheduler now consume current clock values on every tick, with xUnit regression tests. This does not yet reproduce all Windows WPF clock formatting or provider variables; media, sensors and HA are pending. Fixed PageTest sending invalid unsaved content. Commits 11e068c1, 8012c5f0, da0b67c1; CI pending.

- Data-source compatibility block: the Avalonia project now compiles the **original** `LIS2.Sources/IDataSource.cs`, `DataSourceRegistry.cs`, and `ClockDataSource.cs` as linked source, without referencing Windows-only LibreHardwareMonitor. `PageSourceValues` starts/stops the original clock, snapshots `Clock.Time`, `Clock.Date`, `Clock.Day` (original semantics), supplements newer clock components and glyph keys, and is consumed by the preview and running virtual sequence. A variable picker supports discovering keys and inserting templates at the caret on either display line. Source lifecycle and template compatibility tests added. This is a transitional source-sharing strategy, not yet a dedicated shared source assembly. Sensor, HA and media sources are **not** integrated; live GUI remains unverified. CI pending.

- Home Assistant data-source integration: Avalonia now compiles the **original** `HomeAssistantDataSource`, `HomeAssistantEntityInfo`, and `HomeAssistantValueConverter` from `LIS2.Sources` as linked sources (no Windows-only sensor dependency). The original read-only HA WebSocket client, state_changed handling, reconnect and entity attributes feed the shared `DataSourceRegistry` under existing `HA.<entity_id>` variable names. The Pages workspace exposes URL/token connect, test, disconnect, a filterable entity picker and caret-aware state-variable insertion. Token entry is masked and cleared after connection; **no secure persistence or existing Windows settings migration yet**, so configuration must be re-entered each session. Disconnected HA tests added; live server integration and desktop UI have NOT been tested. This is a functional interim panel in Pages, not yet a WPF 1:1 layout. CI pending.

- Portable workspace persistence block: `PortableWorkspaceStore` saves Pages and the Home Assistant URL in `LocalApplicationData/LIS2ControlCenter/avalonia-workspace.json` using temporary-file replacement and serialized writes; malformed JSON is quarantined. Pages are restored on startup and saved after editor changes, reordering, import, add, duplicate, delete. HA access tokens are deliberately **never** stored; Windows DPAPI-protected secrets cannot be transparently decrypted on Linux, and secure cross-platform credential persistence is still pending. The portable workspace is a new schema and **does not yet import original Windows settings.json automatically**. Unit tests cover round-trip, absence of token fields and corrupt-file recovery. CI pending.

- Explicit Windows settings migration: `WindowsSettingsImporter` reads the **original** `settings.json` in a read-only file picker and validates pages using `PageWorkspace`, imports eight original `CustomGlyphs` slots, HA URL and entity preferences. Import is user-triggered, not automatic; source Windows file is never modified. Original DPAPI `HomeAssistant.AccessToken` is **not read or migrated**, and must be entered again. Portable workspace now persists imported glyphs and HA preferences, with autosave after glyph edits/imports. Migration tests cover schema, invalid pages/glyph count, round-trip, and secret exclusion. **Not yet imported:** transport/brightness/theme/language, fan configuration, sensor preferences and other Windows-only fields. This is not full settings parity. CI pending.

- CI investigation: workflow run `38420805175` failed on both OSes at commit `66701582` because `WindowsSettingsImporter` referenced `PortableWorkspace.CustomGlyphs` and `HomeAssistantEntityPreferences` before the next commit added those properties. Subsequent runs through `38421112925` passed on both OSes; the red intermediate commit does not reflect the final state.
- Windows preference migration extension: original `BrightnessPercent`, `ProgramCustomGlyphsOnConnect`, `ThemeMode`, and `LanguageMode` now parse into the portable workspace and survive restart. Glyph-on-connect checkbox is applied and saved; brightness/theme/language values are **retained only**, not yet applied to full UI/platform behavior. Serial COM port and other platform-specific values are intentionally not imported. New tests added; CI pending.


- 2026-10-13 migration hardening: partial Windows JSON imports now merge with a copied current portable workspace instead of resetting omitted fields; page definitions are prevalidated before UI state changes; explicit import propagates persistence failures rather than reporting success. Added partial-import regression test. Physical UI and runtime validation remain outstanding; CI for this change must be checked.

- 2026-10-13 follow-up: Windows settings import now captures the current workspace once, validates the candidate, persists it before modifying the UI, and attempts disk/UI rollback if application fails. This avoids partial UI changes on save errors; rollback itself can still fail on underlying I/O errors. CI and real desktop behavior pending verification.

- Serial transport foundation: added `SerialPortDiscovery` with Linux `/dev/serial/by-id` preference, duplicate suppression and fallback to `SerialPort.GetPortNames`; added explicit-connect `SerialDisplaySession` wrapping the original 19200/8N1 transport for display-only commands, with no automatic reconnect or fan commands. Added discovery regression tests. This is NOT wired to the Avalonia UI yet; no hardware tests or fail-safe validation, and real-device fan control remains disabled. CI pending.

- Serial UI integration: separate experimental controls in Avalonia shell for port discovery/selection, explicit connect/disconnect, writing both display lines, and clearing the display. Serial session is disposed on window close. Virtual display/pages/glyphs remain separate; fan control remains disabled. No auto-reconnect or real hardware verification. CI pending for this integration.

- Larger serial-display integration batch (2026-10-13): extended the display-only session to support both lines, page frames, 8 CG glyph slots, and brightness; added a virtual-transport-injectable constructor for headless tests; wired explicit page/glyph/brightness buttons to the separate serial session, without redirecting virtual playback or enabling fan control. Added serial-session regression tests for explicit lifecycle, validation and no fan command in tested operations. Real hardware, reconnect safe-fan sequencing, full UI parity and CI confirmation remain outstanding.

- Serial page-sequence block: added independent explicit serial sequence start/stop/status using the same `PagePlayback` scheduler as the virtual display, 250 ms polling, frame deduplication, stop on write errors/disconnect, and disabling manual serial operations during playback. Fan commands remain unavailable. Serial playback does not yet implement all per-page scroll/refresh behavior, physical disconnect monitoring, or real hardware tests. CI pending.

- Playback correctness batch: shared `PagePlayback` now honors each page's refresh interval while still switching pages immediately; both virtual and serial sequences inherit this behavior. Serial UI locks all manual display/glyph/brightness controls while sequence writes are active, and clears stale serial frames when no page remains visible. Added deterministic refresh/page-switch regression tests. Scrolling parity and physical fan-safe reconnect remain outstanding; CI pending.

- Overflow integration: shared `PagePlayback` now uses the existing `LIS2.Display.PingPongScroller` independently per line, honoring PingPong/Marquee/Truncate, scroll step interval, edge pause, and resetting scroll position on page changes. Template value refresh remains separately throttled by each page's refresh interval, while scrolling continues between refreshes. Added deterministic tests for modes, independent lines, and reset on page switch. Both virtual and serial playback use this shared engine. CI and hardware behavior pending verification.

- Headless frame dispatch extraction: added `PageFrameDispatcher` (serialized writes, duplicate-frame suppression, one-time clear for no visible page, failed-write retry), reused by virtual and serial page playback. Added headless regression tests including concurrent dispatch and failure/retry; guarded playback restart while an output write is in flight. UI timer orchestration remains in Avalonia and is not yet a standalone runtime service. CI pending.

- Coherent runtime lifecycle batch: extracted shared UI-independent `PageSequenceController` combining page scheduling, deduplicated/serialized output, start/stop/restart protection, failure shutdown and visibility-driven clearing. Both virtual and serial Avalonia adapters now use this controller; UI retains only DispatcherTimer host ticks and presentation/status. Added lifecycle, deduplication, visibility, error and in-flight stop tests. Fan control remains intentionally unavailable; physical hardware untested. CI pending.\n\n- Lifecycle safety batch: added `PageSequenceController.StopAndDrainAsync()` for shutdown to await in-flight display output, guarded manual virtual display actions against active writes and stop playback before manual takeover, disabled conflicting virtual controls while playback runs, and added drain lifecycle tests. Window shutdown now drains virtual and serial page output before session disposal. Fan commands remain out of scope; physical validation outstanding. CI pending.\n\n- Headless periodic runtime batch: replaced virtual, serial and Home Assistant UI `DispatcherTimer` instances with a shared cancellation-aware `PeriodicRuntimeLoop` using `PeriodicTimer`. The loop has explicit start/stop/drain, rejects duplicate starts, and is unit tested for in-flight shutdown and restart. Avalonia dispatcher remains only as a UI-thread bridge for snapshots and UI updates. This is **not** yet full runtime DI/source ownership or fan integration; no hardware tested. CI pending.\n\n- Headless display runtime ownership batch: added `DisplayRuntimeHost` to own `PageSourceValues`, deterministic clock, virtual/serial `PageSequenceController` instances, common snapshot/tick entry point, and orderly shutdown of in-flight writes before source disposal. Both UI playback adapters now use the same runtime, with lifecycle and ownership regression tests. Platform-specific dispatch timers remain adapters; fan control and full DI are still outstanding. CI pending.\n\n- Runtime tick synchronization hardening: periodic virtual/serial/HA UI work is now awaited through `UiRuntimeDispatch` instead of fire-and-forget `Dispatcher.Post`; draining periodic loops therefore waits for dispatched work, and cancellation skips stale queued ticks. HA timer is drained during window shutdown, serial disconnect drains playback before disposing the session, and failed periodic callbacks can be drained and restarted. Regression test covers callback failure/restart. Hardware fan commands remain untouched; physical validation pending.\n\n- Shared runtime-core integration: new portable `src/LIS2.Runtime` project owns clock/HA source lifecycle and configuration under an async gate, exposes source errors, and defines platform capability interfaces (media, sensors, serial discovery, tray, autostart, secrets, appearance). Avalonia now consumes the runtime project rather than compiling source files itself; template variable keys remain unchanged. Added lifecycle and compatibility tests. WPF remains unchanged. The runtime core is not yet the complete WPF orchestration/fan extraction; that remains open. CI pending.\n\n- Process correction (2026-10-13): PRP commit gate hardened after repeated incomplete commits. Phase 1 Runtime Core (steps 1–4, 6) remains OPEN after `1e0dc5d7`; next code push must complete the remaining agreed Runtime Core scope as one cohesive commit. Integration & Safety (steps 5, 7, 8) is separate and OPEN. No further partial code pushes permitted without explicit owner-approved scope change.\n\n- Architecture correction (owner directive, 2026-10-13): WPF is a read-only migration reference, NOT a runtime adapter or supported future UI. Single end-state is Avalonia on Windows/Linux/ARM64; mandatory WPF removal after parity. Runtime Core and Integration & Safety target Avalonia only. Strict complete-package code-commit gate remains in force.\n\n## Verification and limitations
- Static scan of the three library trees found no direct WPF, WinForms, Registry, WinRT, or P/Invoke usage.
- The `System.IO.Ports` dependency remains and actual serial device behavior on Linux is **not** verified.
- No .NET SDK is installed in the current execution environment; local `dotnet build` / `dotnet test` could not be run.
- Run 38406414651 was inspected including job logs; earlier AVLN3000 failure is fixed. New virtual UI integration still requires CI validation and real desktop smoke testing.
- User reported the previous CI run green; newly changed WPF-aligned UI must still pass its own Windows/Linux build and GUI startup checks.
- The Windows release workflow and WPF source files have not been modified.

## Next work
1. Confirm Ubuntu workflow success and Windows solution CI.
2. Introduce platform-neutral application runtime with tests while preserving WPF behavior.
3. Confirm the virtual-session Avalonia shell builds and launches on Windows and Linux; add automated virtual-device tests and shared view models.
4. Implement Windows and Linux providers, then UI feature parity and physical-device safety validation on both systems.

## Pull request gate
Open a PR against `main` only when a coherent, tested slice is ready. Keep this branch isolated until then.

- Runtime Core consolidation candidate (2026-10-13): moved portable source/page/scheduler/output/clock/periodic ownership into LIS2.Runtime through linked compile files, introduced adaptive display driver and shared priority event queue for Avalonia virtual/serial output; added headless deadline, priority, lifecycle and drain regression tests. WPF untouched and is reference only. Validation required before marking package complete.

## Fan safety phase 1 (headless; hardware disabled)
- Added LIS2.Runtime.FanRuntime with four-channel configuration snapshots, existing LIS2.Fans.FanController calculation, finite sensor validation, curve/hysteresis history, explicit opt-in output, write-on-change, serialized ticks, reset on disconnect/reconfigure and safe failed-write retry.
- Added IFanSensorSource/IFanOutputSink and deterministic SimulatedFanSensors/SimulatedFanOutput; FanRuntimeLoop provides periodic evaluation and drain on shutdown. No physical serial fan writes or Avalonia fan control are enabled by this phase.
- Headless tests cover dry-run, all four outputs, modes, invalid sensors/fail-safe, hysteresis, reconfigure, disconnect, failed writes and concurrent ticks. CI and real-hardware validation are separate; Phase 2 must connect platform sensors, hardware, UI and settings.

## CI repair checkpoint (2026-10-13)
- Fan Phase 1 commit `5b6156d` failed Actions run `38437023413` on both Avalonia jobs; portable library jobs green. Identified `CS0136` in `src/LIS2.Runtime/FanRuntime.cs` due to conflicting `raw` local declarations. Repair required; package A remains OPEN until newest CI green.
- PRP execution protocol revised to explicitly capture owner-approved fan package A (headless) and B (hardware+UI), exact CI-first workflow, handover requirements, and no partial pushes within a package.

## Fan settings compatibility slice (2026-10-13)
- Portable four-channel fan settings preserve original Windows JSON property names and defaults, normalization, mode, sensor key, curves, limits, hysteresis and allow-stop; Windows settings import now includes Fans and portable workspace persists them.
- Regression tests cover default values, Windows import, invalid data normalization and portable roundtrip. No fan hardware or automatic control is activated by importing AutomaticControlEnabled; physical integration and Avalonia fan UI remain OPEN.
- Current CI validation required for this commit.

## Fan Control Avalonia editor and simulation (2026-10-13)
- Added four-channel Fan Control screen for modes, sensor keys, fixed/limits/fail-safe/hysteresis, stop permissions, curve points, persisted automatic preference and safe simulated sensor temperature preview using headless FanRuntime.
- Physical fan output stays DISABLED; no SetFans serial command is wired, no real sensor provider is assumed. Actual sensor/serial integration and hardware validation remain open.
- This commit needs Windows and Linux CI before acceptance.

## Hardware integration groundwork — 2026-10-13
- Added Linux hwmon temperature snapshots with stable device/index and optional label aliases; absent/invalid sensor readings omitted for runtime fail-safe.
- Added explicit-authorization serial fan sink: validated four-channel safety frame is first write after connect, no automatic reconnect, failed write revokes authorization, disconnect blocks output; virtual transport regression tests.
- IMPORTANT: SafeSerialFanSession is NOT wired into SerialDisplaySession or UI. No physical output is enabled; WPF parity, combined serial arbitration, automatic regulation lifecycle and real hardware tests remain OPEN. Package 1B must not be called complete yet.

## Single-owner serial integration contract (2026-10-13)
- Introduced UnifiedLis2Session with one Lis2Device and one session semaphore for fan/display command ordering; explicit display-only or safe-frame-first fan connection, fan write revocation on errors, no automatic reconnect, serialized disconnect/dispose. Virtual protocol regression tests cover ordering and authorization.
- IMPORTANT: This is a tested integration building block, not yet the live Avalonia session. UI still uses display-only SerialDisplaySession. Real sensors, periodic fan loop, UI lifecycle and hardware verification remain outstanding; phase 2 remains OPEN.

## Unified display routing (2026-10-13)
- Live SerialDisplaySession display calls now delegate to UnifiedLis2Session for single-owner serialization. Added virtual transport tests for display facade and concurrent display/fan commands.
- Physical fans remain disabled in UI; periodic real regulation and fan authorization lifecycle remain open. Package 1B is NOT complete.

## Fan channel editor regression repair (2026-10-13)
- Channel selection now saves the previously edited channel rather than overwriting the destination channel; invalid edits restore the original selection without losing editor contents. Phase 2 remains open for live runtime integration and physical safety verification.

## Live Linux hwmon sensor preview (2026-10-13)
- Fan UI can now list current Linux hwmon sensor keys and calculate all four channel outputs from real read-only sensor snapshots without sending physical fan commands. Missing sensors continue to trigger runtime fail-safe. Windows displays no hwmon sensors (Windows provider still pending). Explicit hardware enable, continuous periodic fan runtime and safe lifecycle remain open.

## Fan runtime loop fault isolation (2026-10-13)
- The periodic fan loop now disables physical output on an unhandled tick/write exception and exposes LastFault/Faulted instead of silently terminating. No automatic reconnect. Live hardware activation remains disabled pending UI integration and safety validation.

## Serialized safe shutdown (2026-10-13)
- Authorized unified device sessions attempt their validated four-channel safety frame on disconnect and disposal, under the shared display/fan command gate, before transport closure. Virtual transport regression tests cover both paths. A physical write failure still propagates and transport closes; actual fan state cannot be guaranteed. Full fan UI activation and end-to-end lifecycle remain open.

## Explicit fan runtime session lifecycle (2026-10-13)
- Added AuthorizedFanRuntimeSession, an opt-in headless coordinator for unified serial safe-frame initialization, immediate four-channel evaluation, periodic FanRuntimeLoop, fault exposure, and ordered stop/drain/safe-frame disconnect. Virtual transport test checks first/last frames and initial regulated frame. NOT connected to the Avalonia live UI; physical hardware remains disabled by default. Device validation and further failure-mode tests remain required.

## Sensor-provider I/O fail-safe (2026-10-13)
- Sensor snapshot exceptions are treated as missing readings, so sensor-dependent channels still calculate and (when explicitly enabled) write their configured fail-safe outputs rather than silently stopping regulation. Added regression test for an I/O-failing provider. Physical fan output remains UI-disabled pending complete end-to-end safety acceptance.

## Fault containment and disconnect attempt (2026-10-13)
- Background fan-loop errors now preserve the original fault even if output disable or event subscribers throw. Ordered stop attempts output disable and serial disconnect even when loop draining fails. Actual physical output state still requires hardware validation.

## Continuous read-only fan monitoring (2026-10-13)
- Avalonia fan editor now supports start/stop of a 2-second continuous four-channel sensor-driven dry-run, with fail-safe indications, cancellation, and shutdown drain. The monitoring output uses SimulatedFanOutput exclusively; it never opens a serial port or authorizes hardware fans. Live physical fan activation remains gated pending complete safety review.

## Dry-run monitor shutdown race (2026-10-13)
- StopFanMonitorAsync now retains the running task and cancellation source until its completion, preventing a new preview from starting while the old one is still draining. This is read-only UI lifecycle hardening; hardware fan output remains disabled.

## Safety-frame retention after revoked authorization (2026-10-13)
- The safe shutdown attempt now uses the retained safety frame whenever a device remains connected, even if a failed fan write has revoked authorization. This avoids silently skipping safety restoration after an output error. Physical hardware validation remains outstanding.

## Virtual fault-injection regression tests (2026-10-13)
- Added simulated one-shot serial write failures to verify fan authorization is revoked and the previously established safety frame is still written during both explicit disconnect and disposal. No physical device is required. Owner has explicitly deferred physical-hardware validation; virtual transport, CI and platform builds are the current verification scope.

## End-to-end virtual fan control in Avalonia (2026-10-13)
- Fan editor now offers a separate virtual LIS2 four-channel runtime: explicit start, safe 100% frame before first regulation, simulated sensor inputs, periodic regulation, stop/drain and restored safety frame. Dedicated VirtualLis2Transport never opens a physical port. The virtual session is disposed on window close. Added missing-sensor fail-safe + safety-restoration integration test. Physical fan UI authorization remains disabled and is not claimed complete; owner deferred physical validation, not safety implementation.

## Safe disposal after revoked fan authorization (2026-10-13)
- Fixed UnifiedLis2Session.DisposeAsync to attempt the stored safe frame whenever the transport remains connected, even after a failed fan write revoked authorization. Added explicit regression for revoked authorization followed by disposal. This closes the discrepancy with DisconnectAsync; virtual CI verification pending.

## Shared live serial fan/display integration (2026-10-13)
- SerialDisplaySession now owns an optional FanRuntime/FanRuntimeLoop over the SAME UnifiedLis2Session and serial transport used for display. Explicit UI checkbox and start/stop buttons enable fan writes only on a connected port; consent is not persisted and is cleared on start. Safe 100% frame precedes the first regulated write, stop drains periodic output and restores the safe frame without closing the display, disconnect/dispose drain fans first. Display commands are serialized through the unified gate. Added virtual transport regression for display -> fan authorization -> regulation -> safe revocation -> continued display. Windows temperature provider remains unavailable; missing sensor keys fail safe. Real hardware validation deferred by owner. CI pending.

## Shared serial session regression coverage (2026-10-13)
- Added end-to-end VirtualLis2Transport tests through SerialDisplaySession verifying a single connected display port, initial safety frame, regulated frame, interleaved display commands, stop without disconnect, and disconnect while fan loop is active. UI disables start/stop during a fan transition. CI validation required.

## Windows sensor provider (2026-10-13)
- Added WindowsHardwareFanSensors using the original WPF LibreHardwareMonitorLib 0.9.6 and compatible temperature keys. Snapshots update sensors synchronously rather than using stale cached readings; invalid/missing readings fail safe. Windows provider is disposed on Avalonia window shutdown. Linux continues using hwmon. CI compilation and Windows hardware-dependent runtime behavior still require validation; physical LIS2 device validation deferred.
