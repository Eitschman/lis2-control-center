# Windows + Linux shared Avalonia port — implementation status

Date: 2026-10-13
Branch: `feature/linux-avalonia`
Status: **Active incremental port; CG Builder and Pages editor available in virtual transport; latest CI must be verified**

## Binding validation clarification (owner, 2026-10-14)
- No real LIS2 hardware is available, including during the WPF era. Physical tests are not a technical package acceptance gate. Keep virtual protocol and fan safety regression tests; hardware remains unverified.
- Owner desktop tests and visual screenshot acceptance are deferred until the complete Avalonia port is implemented. Do not require interim manual testing or desktop screenshots for intermediate package acceptance.
- Each package is accepted by completed code scope, WPF source parity audit, automated regression tests, and successful latest-HEAD CI. Final product acceptance and WPF removal remain gated on owner desktop testing of the completed port.

## Binding UI acceptance criterion
- User confirmed 2026-10-13: The existing Windows WPF UI is the authoritative reference for **layout, placement, interactions, workflows and visual appearance** on BOTH Windows and Linux. The Avalonia sidebar/virtual buttons are temporary engineering scaffolding, not a proposed redesign.
- Before marking a screen implementation complete, compare controls and behavior against WPF source and document differences. Matching-resolution/DPI desktop screenshot acceptance is deferred until the complete port is implemented.

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

## Fan loop fault recovery (2026-10-13)
- FanRuntimeLoop now supports an awaited fault cleanup callback. The shared serial fan session uses it to revoke hardware authorization and restore the stored 100% safety frame when a periodic tick fails; StopFansAsync/DisconnectAsync remain responsible for draining and retrying restoration on failure. No automatic reauthorization. CI pending.

## Initial authorization I/O failure hardening (2026-10-13)
- On an already-connected display session, retain the 100% safe frame before attempting the initial fan authorization write. A failed initial write leaves fan authorization false, prevents regulated writes, and disconnect retries the safe frame before closing the transport. Added regression with an injected serial write failure. CI pending.

## Serial fan fault notification and UI transition guard (2026-10-13)
- Forward background fan loop faults to the Avalonia UI, showing an explicit safety-restoration-attempt warning. Guard hardware start/stop against overlapping serial actions; retain session ownership through disposal. Safety restoration on serial I/O failures is best-effort, not guaranteed without hardware verification.

## Connect-with-safety failure containment (2026-10-13)
- Retain the safe fan frame before the first connect-time write and attempt a safety-frame retry before transport close if that initial write fails. The safety attempt is best-effort; physical verification remains deferred by owner.

## Paket 1 acceptance audit (2026-10-13)

**Result: NOT ACCEPTED.** Audit performed against `docs/prp/linux-avalonia-port.md` and the WPF/Avalonia source. Hardware tests are explicitly deferred by owner and are not part of this audit.

| Gate | Evidence | Result |
|---|---|---|
| Windows + Ubuntu CI | `f5f41cf9555bb5966310285533646b283e061f63`, Actions run `38452801359`: all four jobs successful | PASS |
| Headless four-channel fan runtime | Runtime, simulated sensors/sink, regression tests and successful CI | PASS (automated scope) |
| Single-owner serial fan/display, authorization and shutdown | `UnifiedLis2Session`, `SerialDisplaySession`, virtual transport tests, successful CI | PASS (automated scope; physical state unverified) |
| Windows/Linux temperature providers | `WindowsHardwareFanSensors`, Linux hwmon source and dry-run UI present | PARTIAL: sensor identity/alias and freshness parity not fully proven |
| Fan editor/configuration | Four-channel editor, persistence, simulated and live dry-run, explicit opt-in | PARTIAL: WPF interaction/layout parity not verified |
| Complete WPF-equivalent Avalonia UI | WPF `MainWindow.xaml` vs Avalonia `MainWindow.axaml` have different navigation/device layout and missing comparable controls | FAIL |
| Visual and interactive screenshot parity | No matched-resolution WPF/Avalonia screenshots or interactive desktop acceptance evidence | NOT TESTED |
| Physical LIS2 hardware | Explicitly excluded by owner | EXCLUDED |

**Blocking work:** port the WPF layout and screen interactions to Avalonia without redesign; create per-screen parity matrix, automate interaction/screenshot comparisons and run desktop smoke checks; close outstanding sensor-key compatibility and error-path coverage. The existing four green CI jobs prove compilation/tests, not visual/functional UI parity. Do not mark package 1 technically done until implementation and automated regression gates pass; owner desktop screenshots are deferred until the full port is complete. WPF stays read-only reference and main remains unchanged.

## Navigation and transport parity package (2026-10-14)
- Replaced the Avalonia ListBox navigation with ten WPF-order button entries and explicit selected-button styling.
- Restored the WPF-like sidebar transport summary and Change action to Settings; moved experimental serial/virtual transport buttons to the Settings view without removing their existing event handlers.
- This is structural navigation parity only: theme, localization, desktop screenshot and full Settings-screen parity remain unverified. Physical hardware remains excluded.
- CI validation must be recorded against the new HEAD; no claim of desktop visual parity.

## Hardware sensor browser (2026-10-14)
- Added a dedicated Avalonia Hardware view with read-only platform sensor discovery, key filtering, snapshot refresh and selected-key transfer into the current fan editor.
- Assignment does not persist automatically or authorize physical fan output; the user must explicitly save fan configuration.
- This is a first functional Hardware slice, not WPF Hardware-screen parity (aliases, favorites, availability, live updates, grouping and visual QA remain open).

## Hardware sensor preferences (2026-10-14)
- Added editable aliases, favorite flags and favorites-only filtering to Avalonia Hardware sensor browser.
- Preferences persist in the non-secret portable workspace and are loaded with backward-compatible empty defaults; selected sensor assignment remains explicit and non-activating.
- Remaining: WPF hardware grouping/availability/live polling parity, automated UI testing and real-device verification. CI validation: commit `7cbae9a` passed all four Windows/Ubuntu jobs (run `38519777968`).

## Hardware live snapshot refresh (2026-10-14)
- Hardware sensor browser now offers explicit 2-second live snapshot refresh, stop/start controls, and stops its UI timer during window shutdown.
- Existing alias/favorite filtering and selection are retained across refreshes by stable sensor key. No serial writes or physical fan activation.
- Remaining WPF parity: sensor grouping/availability, full hardware source coverage and desktop visual/interaction verification. CI: commit `883bd09` passed all Windows/Ubuntu jobs (run `38520076241`).

## Hardware sensor availability and tests (2026-10-14)
- Deterministic read-only catalog projection with case-insensitive keys, favorite-first sorting, alias/key filtering and unavailable placeholders for saved missing sensors.
- Missing sensors cannot be assigned to fans. Added catalog and portable-workspace regression tests. No hardware write path introduced.
- Full WPF visual parity and hardware validation remain open. CI: commit `5e2bf7c` passed all Windows/Ubuntu jobs (run `38521151712`).

## Hardware preference safety and availability refinement (2026-10-14)
- Corrected case-insensitive preference matching, blocked unavailable sensor assignment in the click handler (not only button state), improved unavailable counts, and added save-failure handling to avoid leaving modified preferences in memory when persistence fails.
- Added catalog regression assertions for case-insensitive deduplication and unavailable favorite alias search.
- CI for this commit pending; visual parity, WPF grouping, full hardware sensor coverage and physical verification remain open.

## Owner validation clarification and latest CI (2026-10-14)
- Confirmed latest HEAD `caf703f` green on Windows/Ubuntu (Actions run for `caf703f`; four jobs passed).
- Physical LIS2 hardware testing is unavailable, as it was for WPF. Do not use it as a package gate; retain virtual transport and simulated fan safety tests.
- Manual desktop and screenshot checks are postponed until the complete Avalonia port is implemented. Technical package acceptance relies on code parity audits, headless tests, and CI. WPF retirement awaits final owner desktop acceptance.

## Diagnostics navigation and read-only runtime state (2026-10-14)
- Connected the previously placeholder-only Diagnostics navigation item to an actual read-only Avalonia view.
- Added manual refresh of platform, virtual and serial connection state, page playback, fan dry-run monitoring and virtual command count; no device commands or fan authorization are issued.
- Extracted a pure diagnostics formatter with headless regression tests. This is a first Diagnostics slice, not complete WPF diagnostics parity. Current-HEAD CI must pass before technical acceptance; owner desktop checks remain deferred.

## Hardware preference transaction correction (2026-10-14)
- Preference-save failure rollback now distinguishes newly created entries from existing empty preferences, so failed persistence cannot delete a previously saved blank preference.
- Sensor transfer navigates before writing the fan editor key; no fan configuration is persisted or physical output authorized by the transfer.
- No manual desktop or physical hardware tests required during port implementation under the binding 2026-10-14 owner clarification.

## Diagnostics build repair (2026-10-14)
- Run `38522848177` failed both Avalonia jobs with CS1061 in `MainWindow.Diagnostics.cs`: missing `using Avalonia.Controls;` for `FindControl` extension method. Shared .NET jobs passed.
- Applied minimal namespace import repair; Repair commit `cffa3a8` passed all CI jobs. No changes to WPF or fan safety.

## Diagnostics runtime snapshot expansion (2026-10-14)
- Extended read-only diagnostics with serial busy state, hardware sensor live-refresh state, and count of fan channels with assigned sensor keys (not a claim that sensors are currently available).
- Expanded headless tests for active and inactive snapshots; no serial writes, hardware access, or authorization transitions introduced.
- WPF diagnostics feature parity is still pending; current-HEAD CI must pass. Manual desktop acceptance remains deferred until complete port.

## Hardware sensor live-refresh editor preservation (2026-10-14)
- Suppressed selection-change form resets while the sensor list is rebound during manual and two-second live refresh; unsaved alias/favorite edits are preserved when the same sensor remains selected.
- On a filtered-out or missing selection, clear the editor and disable preference save to avoid accidentally applying stale edits to another sensor.
- Normalize case-insensitive sensor snapshot keys in one dictionary pass (including duplicate keys with different case); added headless regression coverage.
- No hardware writes or fan authorization changes. Full WPF visual parity remains pending; current-HEAD CI required for technical acceptance.

## Hardware WPF type-filter parity (2026-10-14)
- Restored the original WPF Hardware type filter categories (All types, Favorites, Unavailable, Temperature, Load, Fan, Clock, Voltage, Power, Data, Throughput, Other) alongside existing text and favorites filters.
- Categorization uses explicit sensor-key path segments, not guessed hardware values; unavailable saved sensor keys remain filterable. This is a partial WPF parity slice; WPF grouped rows and full monitoring details remain pending.
- Added headless filter regressions. No fan authorization or hardware write path added. Current-HEAD CI pending.

## Hardware sensor column parity (2026-10-14)
- Replaced plain-text hardware list with WPF-inspired five-column rows: markers, sensor alias/name, type, value and template key; header and row data projections added.
- Centralized sensor type detection for filtering and row rendering, with headless tests. WPF grouping and full visual parity remain open. Current HEAD CI pending.

## Hardware column layout CI repair (2026-10-14)
- Commit `06bbb20`, run `38524643627`: both Avalonia jobs failed AVLN2000 at `MainWindow.axaml:403`, because WPF's `HorizontalContentAlignment` is not a supported Avalonia ListBox property. Both shared-library jobs passed.
- Removed the invalid ListBox property; the row Grid keeps explicit five-column layout. CI for the repair commit is pending; no hardware behavior or fan authorization changed.

## Hardware four-channel fan assignment parity (2026-10-14)
- Replaced the single current-channel assignment action with WPF-matching Fan 1, Fan 2, Fan 3, Fan 4 buttons. The selected available sensor is copied into the explicitly chosen fan editor channel; selection and assignment never persist settings or authorize hardware output.
- All four buttons share availability gating. Display-preset generation and remaining WPF hardware grouping are still open. Current HEAD CI pending; desktop checks deferred until complete port.

## Hardware display preset editor workflow (2026-10-14)
- Added WPF-named CPU/GPU/Memory/Temperatures/Fans preset actions to Avalonia Hardware.
- Pure preset builder selects available snapshot keys by explicit key segments, and creates editable display pages with sensor placeholders; pages are persisted to portable workspace and editor navigation opens afterward.
- Added headless preset selection/template tests. WPF preset page composition and placeholder compatibility require further source comparison; full parity is NOT claimed. No device commands or fan authorization.
- Current HEAD CI pending; no manual desktop checks until full port.

## Hardware display presets: WPF source parity correction (2026-10-14)
- Compared actual WPF `CreateHardwarePresetPage`: each preset creates **one** page, not one page per sensor. Replaced the initial Avalonia per-sensor implementation with WPF-like CPU/GPU Load+Temperature, Memory Load+Data, and first-two Temperatures/Fans selection, with favorite priority.
- Numeric live sensors only; exclude .Unit metadata; append per-sensor .Unit when available. Empty preset selection creates no page. Created pages have 5-second duration and PingPong overflow, matching WPF.
- Replaced initial preset tests with exact template/favorites/empty-case regressions. Physical device writes remain untouched; current HEAD CI pending. This is source-level behavior parity, not final desktop acceptance.

## Hardware tracked sensor references (2026-10-14)
- Compared WPF tracked-key behavior: referenced Hardware.* placeholders from display pages and fan channel sensor keys now remain visible when disconnected, marked unavailable. Availability requires a numeric reading; assignment remains disabled for missing sensors.
- Exposes separate hardware and sensor display names in the catalog. Added headless regressions for disconnected references and nonnumeric values. Metadata filtering and WPF grouping remain open; no hardware output or authorization change.
- CI acceptance pending for this HEAD; final desktop testing deferred.

## Hardware grouping and metadata parity (2026-10-14)
- Hardware list now shows hardware group headers with favorites first within each group; the selected sensor key survives refresh and group headers cannot be assigned to fans.
- Excluded .Unit metadata rows, displayed human-readable sensor name below alias, retained full template key. Added headless grouping/metadata regressions. Avalonia typed templates replace WPF GroupStyle. No hardware writes or fan authorization. Latest CI pending; desktop validation deferred.

## CI repair for hardware grouping (2026-10-14)
- Run 38533080462 on d409087 failed both Avalonia builds with CS0117: HardwareSensorEntry.GroupForDisplay missing. The UI call was correct, but the implementation was accidentally omitted from HardwareSensorEntry.
- Implemented the missing grouping method in HardwareSensorEntry with case-insensitive hardware group headers and favorites-first ordering. Shared-library jobs had passed; CI for repair HEAD pending. No device commands or authorization changes.

## CI repair: sensor-key test fixture (2026-10-14)
- Run 38533392908 built both Avalonia targets successfully; one headless test failed on both OSes: CaseInsensitiveSnapshotKeysDoNotDuplicateEntries expected legacy non-Hardware key names to appear after WPF-style sensor-key filtering was introduced.
- Updated the test fixture to realistic Hardware.CPU.Temperature.* keys with mixed casing, retaining its original purpose of checking case-insensitive snapshot merging and saved preferences. No runtime behavior or safety changes; new HEAD CI pending.

## Hardware sensor unit parity (2026-10-14)
- Sensor rows now read the per-sensor Hardware.*.Unit metadata and append its unit to displayed values; Celsius is no longer hardcoded in ToString. Missing units remain unitless; disconnected readings remain unavailable. The metadata itself is not listed as a sensor.
- Added headless regression coverage for temperature, load and fan units, plus unitless power sensors. No hardware write behavior changed; desktop acceptance remains deferred. Latest-HEAD CI pending.

## Hardware sensor filtering and grouping WPF parity (2026-10-14)
- Compared WPF RefreshHardwareSensors: untracked nonnumeric snapshot fields must not appear as phantom unavailable sensors; explicitly tracked/saved sensor keys remain unavailable when missing or invalid. Search now matches hardware name, sensor name, display name, type, alias and full key.
- Within each hardware group rows are ordered favorites first, then available first, then type/display name/key, matching WPF semantics. Added headless regressions for nonnumeric tracked/untracked sensors and multi-field search. No fan output changes; desktop checks deferred; current HEAD CI pending.

## Hardware sensor display-name parity (2026-10-14)
- Compared WPF ParseHardwareSensorKey/HumanizeSensorName: Avalonia hardware and sensor labels now separate camel-case/acronym boundaries and underscores/dashes (e.g. GraphicsCard -> Graphics Card; GPUHotSpot -> GPU Hot Spot). Stable raw keys remain unchanged for templates, saved aliases, favorites and fan references.
- Added headless display-name regressions; no physical device operations. CI on latest HEAD pending; final desktop checks deferred.

## Hardware type-filter WPF parity (2026-10-14)
- Fixed an actual WPF/Avalonia filter mismatch: WPF uses 'All', while Avalonia catalog previously only accepted 'All types'. Both are now accepted, and Favorites/Unavailable special filters match case-insensitively. Unknown sensor categories still fall under Other.
- Added headless regression tests for All, Other, Favorites and Unavailable; no hardware or fan-write changes. Latest HEAD CI pending.

## Hardware monitoring summary and source error parity (2026-10-14)
- WPF-style hardware monitoring summary now shows visible sensors, total non-unit snapshot values, and unavailable configured sensor count; source errors take precedence and persist across filter changes until the next successful refresh. Snapshot metadata is excluded from the total. Headless regression tests cover filtered counts, unavailable tracked sensors and source errors.
- Hardware writes and fan authorization unchanged. CI pending for this HEAD; manual desktop verification remains deferred until full port.

## Hardware selected-sensor detail panel (2026-10-14)
- Restored WPF's selected-sensor detail readout in Avalonia: selected display name, current value with unit or unavailable state, and the exact stable template key. Details refresh with the sensor list, clear on deselection and follow alias/favorite edits; selection is read-only and never sends device commands.
- Added headless projection regressions, wired selection and refresh handling, and added the view element. Current HEAD CI pending; desktop visual acceptance deferred.

## Hardware sensor invalid-value isolation (2026-10-14)
- Prevented NaN and positive/negative infinity from appearing as live available hardware sensors. Such values are omitted from untracked discovery, but tracked references remain explicitly unavailable, preserving fan/editor and page reference visibility. Malformed nonnumeric values behave identically. No physical fan writes are affected.
- Added headless regression coverage for NaN, infinities, malformed values, and valid numeric snapshots. CI pending for current HEAD; manual desktop checks deferred.

## CI repair after 95a74f1 (2026-10-14)
- GitHub Actions run 38539648454: shared Windows/Linux green, Avalonia Windows/Linux red during test-project build due to xUnit2031 at HardwareSensorCatalogTests.cs:37. Corrected Assert.Single(entries.Where(...)) to Assert.Single(entries, predicate). This is a test analyzer fix only; nonfinite sensor behavior unchanged. CI pending on repair commit.

## Downloadable preview builds for Pi 5 and desktop (2026-10-14)
- Extended cross-platform CI with gated self-contained, untrimmed single-file Avalonia publishes for linux-arm64 (Pi 5), linux-x64 and win-x64. Each runtime-specific package is uploaded as a GitHub Actions artifact for 14 days, only after both shared-library and Avalonia test matrices succeed. Linux archives preserve executable permissions; Windows uses ZIP.
- Preview distribution only: no production release, no WPF removal, no claim of physical LIS2 or desktop acceptance. On Pi 5 extract the downloaded linux-arm64 tar.gz and run ./LIS2.App.Avalonia; .NET SDK is not required. GitHub artifact download may itself be wrapped in a ZIP by GitHub Actions.
- Current HEAD CI pending; publish/runtime compatibility and ARM64 native dependencies must be confirmed by the CI publish job and eventually owner desktop validation.

## Preview publish sanity gates (2026-10-14)
- Added per-RID CI verification that the expected platform executable exists and is not suspiciously small before packaging/upload. Linux archives retain executable mode; the existing shared/Avalonia tests remain prerequisites. This is static artifact sanity checking, not execution on ARM64 or UI acceptance.
- Prior preview publish commit 2649022 succeeded all seven jobs in run 38542030053. New HEAD CI pending.

## First Raspberry Pi 5 screenshot: usability fixes (2026-10-14)
- Owner confirmed ARM64 preview starts on Raspberry Pi 5 KDE/Wayland. Screenshot exposed too-tall minimum window, dark-on-dark sidebar/navigation and status text, and footer overlapping transport controls.
- Reduced default/minimum window geometry to 1200x700 / 850x480, placed sidebar navigation/transport inside a vertically scrolling region with a separate footer, explicitly set light foreground on sidebar/navigation and connection badge, and removed inert help/GitHub/About affordances in favor of a passive preview label. This is a source-based corrective package; new ARM64 screenshot acceptance is pending and no hardware/fan behavior changes.
- CI pending on this HEAD. Existing package workflow continues publishing self-contained ARM64/x64 previews.

## Chrome warning on Actions ZIP preview (2026-10-14)
- Owner reports Chrome flags the GitHub Actions auto-generated artifact ZIP for linux-arm64 as dangerous. The exact Safe Browsing verdict is unknown; a green build is not evidence of browser reputation. No security feature was disabled and no GitHub Release was created.
- Preview packaging changed from self-extracting single-file bundles to conventional untrimmed self-contained multi-file .NET publish for linux-arm64/linux-x64/win-x64. Linux tar.gz and Windows ZIP now include SHA256 checksum files in the Actions artifact; Linux checksum is verified in CI. Artifact download still has GitHub's outer ZIP wrapper, which cannot be disabled with upload-artifact. Chrome warning resolution remains unverified pending owner download test.
- Prior Pi shell CI repair 2f092cc passed all seven jobs; current packaging CI pending.

## Pi 5 dashboard virtual connection flow (2026-10-14)
- Pi 5 preview screenshot confirms window sizing, contrast and sidebar overlap significantly improved. Final WPF parity and full desktop acceptance remain pending.
- Dashboard System Status now exposes explicit Connect virtual LIS2 / Disconnect controls using the existing glyph-aware virtual connection and existing safe disconnect logic. Buttons share the same busy/playback gating as Settings transport controls, and display state/command count comes from a tested pure presentation helper. No serial or physical fan commands added.
- Added headless tests for connected/disconnected virtual dashboard presentation. CI pending for this commit; no new manual desktop acceptance required.

## Strict WPF reference correction (2026-10-14)
- Owner explicitly rejected invented Avalonia functionality. Direct comparison against src/LIS2.App/MainWindow.xaml confirms WPF Dashboard System Status has only a status TextBlock, not virtual Connect/Disconnect buttons. Removed those buttons, their event handlers and enable-state code. Restored original WPF disconnected text and inline status formatting. Virtual transport controls remain in Settings, as before.
- Do not introduce convenience UI or workflows absent from WPF. All future changes must cite the corresponding WPF source control/handler and reproduce its behavior; distinguish platform adapters from new user-facing features. Headless tests and CI remain required. Current CI pending.

## All-ten-menu WPF parity baseline (2026-10-14)
- Compared WPF MainWindow.xaml tabs with Avalonia MainWindow.axaml view declarations. Three WPF menu screens (Media, Events, Home Assistant) lack Avalonia views; the seven existing screens still require detailed behavioral parity verification. Recorded WPF named-control counts, scope and strict no-invention acceptance rules in `docs/prp/wpf-avalonia-parity-audit.md`. Dashboard invented transport buttons already reverted in 2cb3141. This is an audit baseline, not full parity. CI pending.

## Detailed all-menu WPF XAML inventory (2026-10-14)
- Enumerated WPF named controls and XAML event handler names for every menu and checked exact Avalonia identifier presence. Added source-grounded per-control tables to `docs/prp/wpf-avalonia-parity-audit.md`. Identifiers alone do not establish functional parity; renamed Avalonia controls require semantic handler review. No UI code changes; CI not a substitute for this review.

## WPF per-control and handler traceability expanded (2026-10-14)
- Completed named-control and XAML-declared event-handler source traceability for all ten WPF menus (126 named controls, 71 per-tab handlers) with WPF code call evidence and Avalonia candidate mappings in `docs/prp/wpf-avalonia-parity-audit.md` (~25 KB). Three missing views remain Media, Events, Home Assistant; HA has partial embedded Pages functionality. Identified functional deviations in manual fan output, diagnostics history/export, theme/language/startup. This is not yet full semantic parity: unnamed controls, background handlers/timers, validation, persistence and localization still require code-level mapping and regression tests. No runtime changes in this audit commit.

## WPF tab placement regression identified (2026-10-14)

Verified WPF Home Assistant tab owns enable, URL, token, test, save/reconnect, entity/domain filtering, alias/favorites and attribute/page actions. WPF Pages has no HA connection setup. Avalonia currently embeds HA configuration in Pages and lacks HomeAssistantView. Detailed blocking gap recorded in audit and PRP; implementation remains outstanding.

## All-ten-menu WPF placement review (2026-10-14)
- Verified WPF tab headings/actions against Avalonia XAML. Besides HA connection wrongly under Pages, Pages also has non-WPF Windows settings import and playback actions; Fan Control uses additional experimental sections instead of WPF Fan Outputs/Channels; Settings lacks WPF Appearance/Windows startup group; Diagnostics lacks WPF's four-section layout; Media, Events and Home Assistant views are absent. Dashboard/Display/Hardware have similar major sections but require semantic parity review. Findings and corrective requirements recorded in parity audit. No application code changes in this review.

## HA placement correction, partial (2026-10-14)
- Avalonia HomeAssistantView added and existing HA controls moved out of PagesView. Navigation wired. WPF Connection / Home Assistant entities groupings used. Still missing WPF HA enable/save semantics and full entity/attribute management; no claim of parity. Other tab layout discrepancies remain. CI pending.

## HA navigation startup defect fixed (2026-10-14)
- After HA controls moved, InitializeHomeAssistant still attempted to bind a removed HaDisconnect button, causing a null dereference at startup. Removed the obsolete binding and made Save/reconnect respect the WPF Enable Home Assistant checkbox. This is a partial correction, not full HA feature parity. CI pending.

## HA WPF UI group correction continued (2026-10-14)
- Added WPF-named entity-detail and Attributes UI group controls in HomeAssistantView, plus domain selector. Their WPF action semantics are not yet implemented; UI scaffolding is not feature parity. CI pending.

## HA WPF actions wired (2026-10-14)
- Implemented domain filter, alias-aware entity search, favorite-first entity order, selected entity details and attributes, persistent alias/favorite save, entity/attribute clipboard key actions, WPF-style display-page creation. Removed extra HA insert-into-line buttons. Static review complete; CI pending; full WPF parity still open.

## WPF menu ownership batch (2026-10-14)
- Windows settings import no longer lives inside Pages; moved to Settings, preserving its existing handler. Fan Control, Settings, Diagnostics and Pages titles/action labels aligned to WPF where possible. Further per-menu layout/behavior gaps remain; CI pending.

## Diagnostics four-panel correction (2026-10-14)
- WPF four panels restored: Diagnostics Snapshot, Virtual LIS2 state, Decoded command history, Protocol Log; native report export implemented. Command history and protocol log are explicit not-yet-ported placeholders, not fake telemetry. CI pending; WPF behavioral parity incomplete.

## Virtual LIS2 diagnostics port (2026-10-14)
- Real virtual state and last 200 decoded commands now use existing LIS2.Core transport/state/protocol API, following WPF formatting. Protocol Log remains unported; no fabricated entries. CI pending.

## Virtual first test milestone (2026-10-14)
- Auto-connect hardware-free virtual LIS2 on window Opened using existing session initialization; dashboard/pages VFD colors copied from WPF theme brushes (#123B8A/#5D8FE8/#F7FBFF). Test manual Send/Render/Clear and sequence playback with published build. CI pending.

## User test: pages not rendered automatically (2026-10-14)
- User observed manual Display Quick Test works, saved Page 1 ('Hallo'/'Welt') not shown. Root cause: Avalonia sequence only started via explicit Start sequence, whereas WPF starts runtime page rendering and page timer on startup. Fixed by starting existing virtual sequence after auto-connect; Start sequence button reuses same routine. Manual Display actions still stop sequence intentionally. CI pending; real Linux smoke test pending.

## Hardware template values (2026-10-14)
- User test: `Temp {Hardware.cpu_thermal.Temperature.temp1}` rendered only `Temp` although Hardware view detected sensors. Cause: hardware snapshot was not merged into PageSourceValues used by preview and PageSequenceController. Now update the page source snapshot on hardware refresh and clear stale values on failure; refresh variable picker. Real Pi test and CI pending.

## Continuous hardware sampling (2026-10-14)
- Hardware snapshot timer now starts during initialization and samples every two seconds regardless of selected menu, supplying PageSourceValues for virtual/serial template rendering. The Hardware live-updates button controls only list refreshing, not the sampling pipeline. The timer is stopped during window shutdown. WPF hardware-view timer is view-gated, but WPF hardware source updates are separately owned by the source runtime; Avalonia uses the existing cross-platform live sensor adapter. CI and real Pi verification pending.

## Settings sidebar cleanup (2026-10-14)
- User screenshot revealed Windows settings import and page-save status incorrectly placed inside left navigation. Moved import button/status into SettingsView; moved VirtualStatus to SettingsView, leaving transport shortcut in sidebar. No changes to import event wiring or storage. CI pending.

## Linux comprehensive system sensors (2026-10-14)
- Added LinuxSystemSensors combining existing hwmon temperature keys with hwmon fan RPM, /proc/stat CPU utilization (aggregate and per-core delta), CPU cpufreq MHz, /proc/meminfo RAM/swap, /proc/diskstats per-block-device read/write B/s, /proc/net/dev per-interface receive/send B/s, and DriveInfo mounted fixed filesystem capacity/usage. Hardware catalog groups by stable Hardware.<device>.<type>.<metric> keys; template source and continuous sampling reuse the same source. No physical fan outputs authorized. CI and real Pi verification pending.

## HA token persistence (2026-10-14)
- User reported token lost on restart. Existing workspace intentionally did not persist secrets and Connect cleared token input. Added separate AES-256-GCM encrypted credential storage with owner-only Unix key/ciphertext files; never export token to workspace JSON. Rehydrate token input and enabled flag on load; persist only after successful configure, delete on explicit disable. Security limitation: local key is stored alongside ciphertext (not OS keyring), so compromise of user account defeats protection. CI and Pi restart test pending.

## Media implementation started (2026-10-19)
- WPF source audit identified Windows Media Player modern session, Winamp, and WMP Legacy as three distinct integrations. Added Avalonia MediaView with Winamp and WMP Legacy metadata/status and playback-page creation; reuses existing WinampDataSource and WmpLegacyDataSource on Windows only, with periodic UI/template updates. Added PageSourceValues.UpdateMedia for namespaced values (Winamp.*, WmpLegacy.*) and regression test for replacement and template rendering.
- **Not complete / no full parity claim:** Windows Media Player modern session not wired; Winamp/WMP Legacy visualization bars and detailed WPF controls not yet ported; Linux-specific media playback integration not part of original WPF implementation. CI on HEAD and Windows desktop validation pending. Existing media sources may require native plug-ins to provide live telemetry. No physical fan authorization changed.

## Media CI repair checkpoint (2026-10-19)

- Branch `feature/linux-avalonia`; verified implementation HEAD `312be8b306f88b8d470f4af6a21d27bb7697460b` in GitHub Actions run [39261605852](https://github.com/Eitschman/lis2-control-center/actions/runs/39261605852): **success** (Windows and Ubuntu shared libraries and Avalonia shell, plus configured publish jobs).
- Root causes of previous red runs: inherited Windows-only target framework in `LIS2.Sources`, `LIS2.Winamp`, `LIS2.WmpLegacy` caused Linux NETSDK1100 and Windows NU1201; corrected by explicitly targeting `net8.0` in each project. `LIS2.Runtime` additionally compiled linked copies of the same `LIS2.Sources` classes, causing CS0433; replaced duplicate linked source files with a project reference to `LIS2.Sources`.
- Files modified in repair commits: `src/LIS2.Sources/LIS2.Sources.csproj`, `src/LIS2.Winamp/LIS2.Winamp.csproj`, `src/LIS2.WmpLegacy/LIS2.WmpLegacy.csproj`, `src/LIS2.Runtime/LIS2.Runtime.csproj`. No physical device or manual UI acceptance performed.
- **Media package remains incomplete:** Windows modern Media Player GSMTC integration, Linux MPRIS adapter, WPF-equivalent media controls/visualizations, capability-based UI visibility and tests are pending. Preserve `Winamp.*` and `WindowsMedia.*` template keys and existing native Winamp plugin. WMP Legacy and Wine support remain explicitly deferred according to PRP.
- **Single next action:** implement a cohesive cross-platform Media provider/UI parity package based on `docs/prp/linux-avalonia-port.md` Phase 3 and the WPF parity audit; include regression tests, Windows/Linux/ARM64 CI validation and status documentation. Do not mark complete without current HEAD CI green.

## Linux MPRIS implementation (2026-10-19)

- Linux MPRIS metadata adapter added in `LinuxMprisSource.cs`, querying optional `playerctl` without native dependencies. Exposes `Media.Connected`, `Media.State`, `Media.Title`, and `Media.Artist` through `PageSourceValues.UpdateMedia`.
- Avalonia Media tab includes Linux MPRIS status and playback-page creation; headless mapping regression test added. This is an implementation increment, not a claim of complete WPF Media parity.
- GitHub commits: `b4c9e5a`, `30842af`, `29e1fda`, `ac79c2b`. CI pending on latest HEAD. Playerctl availability and real player behavior not verified on Raspberry Pi.
- Open: Windows GSMTC integration, capability-specific card visibility, Windows Winamp spectrum/VU parity, detailed media controls, Linux MPRIS robustness and CI verification. Next actionable work: complete provider/UI parity and confirm latest HEAD green.

## MPRIS metadata completion checkpoint (2026-10-19)

- Confirmed GitHub Actions success for prior HEAD `64aa37e`, run [39263888870](https://github.com/Eitschman/lis2-control-center/actions/runs/39263888870).
- Follow-up implementation: MPRIS album and duration parsing, additional Avalonia fields for album/state/duration, malformed-response regression test. Commits `67ee30a`, `c332178`, `6b4671c`, `53c4e13`.
- Current HEAD CI pending. This increment does not implement Windows GSMTC; the existing Windows-only `LIS2.WindowsMedia` provider requires an architecture that preserves the shared `net8.0` Avalonia target and Windows publish packaging.
- Next task: implement and test Windows GSMTC host bridge; then compare media display controls to WPF and close remaining parity gaps. No hardware or desktop acceptance claimed.

## Windows GSMTC bridge implementation (2026-10-19)

- Prior MPRIS package HEAD `045a40b` confirmed green in GitHub Actions run [39264812311](https://github.com/Eitschman/lis2-control-center/actions/runs/39264812311).
- Added `WindowsMediaBridge` to load the existing Windows-only `LIS2.WindowsMedia.WindowsMediaDataSource` via runtime reflection; preserves the cross-platform `net8.0` Avalonia target and the existing `WindowsMedia.*` variable namespace.
- Connected GSMTC state, title, artist, album, elapsed/duration to Avalonia Media tab and playback-page creation. Windows publish workflow builds the Windows-only source and copies its assembly into win-x64 package. Added missing-provider headless regression test.
- Implementation commits `7bf44eb`, `1960516`, `ece79a8`, `ddafdba`, `ad78904`. **Current HEAD CI not yet verified; do not mark package accepted until green.** Real Windows GSMTC session behavior not manually tested.
- Remaining: full WPF media visual parity, Windows session packaging runtime smoke validation, Linux MPRIS integration on actual player, general WPF/Avalonia parity audit and remaining port phases.

## Media stabilization follow-up (2026-10-19)

- Verified prior GSMTC bridge HEAD `07af626` green, GitHub Actions run [39265470263](https://github.com/Eitschman/lis2-control-center/actions/runs/39265470263).
- Fixed Linux MPRIS `playerctl --format` delimiter/parser mismatch and updated parser regression test to match the real command format.
- Extended Windows publish packaging to include provider output DLL dependencies. Added GSMTC provider availability signal, hides Windows Media panel when bridge DLL is absent, and stops the media refresh timer on shutdown.
- New commits `b1ecc5e`, `6b7e0ae`, `ce193ca`, `9bb31ed`, `849104f`. Latest HEAD CI not yet accepted. Real-device and full WPF visual parity remain open.

## Windows Media navigation crash report (2026-10-19)

- User reproduced a full application crash on Windows when clicking the Media menu. The navigation handler immediately calls `RefreshMedia()`; the precise exception/stack trace was not available, so root cause is not confirmed.
- Hotfix isolates each Windows media provider refresh, contains GSMTC reflection snapshot exceptions, guards Media navigation and Linux MPRIS asynchronous refresh; errors are traced and displayed as provider status instead of propagating through UI event handlers.
- Commits: `0664a2e`, `fe2806d`, `4221fdf`, `f4e36a4`. CI and Windows interactive reproduction are required before closing this incident; do not infer runtime success from a green build alone.
- Next: obtain Windows exception diagnostics if crash persists, add a UI navigation smoke test, confirm media page opens with no media players running and with provider DLL missing.

## Settings WPF parity implementation (2026-10-19)

- WPF Settings source reviewed: three cards Device/Transport (Virtual/Serial, COM, Refresh, Apply/reconnect), Appearance (System/Light/Dark theme; System/English/German/French/Turkish/Russian language), Windows (start minimized to tray).
- Avalonia Settings now exposes the same primary card structure above the existing advanced serial/import diagnostics. New `MainWindow.Settings.cs` handles COM discovery and real serial connect, virtual transport, workspace-persisted theme selection, language preference, Windows per-user Run registry startup; `App` honors `--minimized`.
- ThemeVariant switching is implemented but many Avalonia controls currently have hard-coded brushes, so complete light/dark parity is NOT yet achieved. Language selection persists preference but does NOT translate the UI yet. These are explicitly incomplete and must not be marked WPF parity-complete.
- Commits `fd5e324`, `51f27e6`, `e2704d6`, `78e619f`, `97bf0ee`, `03daa13`, `915e3a1`. HEAD CI pending; Windows registry, tray startup, serial reconnect and desktop Settings interactions unverified.

## Settings screenshot parity and language-aware UI (2026-10-19)

- WPF screenshot supplied by owner is the reference for the compact three-card Settings layout. Existing branch commit `71bf7d8` arranged three German cards and hid advanced diagnostics; controls remain in visual tree for existing event wiring.
- CI on earlier `31e79a2` failed on Linux and Windows due to CA1416 Windows registry API without platform guard; corrected in `c19dda9`.
- Replaced hard-coded German Settings labels with dynamic translation for English, German, French, Turkish and Russian; System resolves `CultureInfo.CurrentUICulture`. Settings headings, hints, combo options, action buttons, Windows startup label, navigation sidebar and current section heading update on selection and persisted workspace load. Source: `MainWindow.SettingsLocalization.cs`.
- Remaining: whole-app localization beyond Settings/sidebar; app-wide WPF visual and theme parity; runtime desktop verification and CI of new HEAD. No full parity claim yet.

## Ten-screen WPF/Avalonia screenshot comparison (2026-10-19)

Owner provided LIS2.zip with WPF and Avalonia captures for Dashboard, Display, Pages, Media, Events, Hardware, Fan Control, Settings, Diagnostics, Home Assistant. All ten pairs visually compared. Findings: shared shell dark vs WPF light, WPF green active navigation, WPF compact cards; Events page entirely blank in Avalonia; Fan Control, Hardware, Media, Pages and Home Assistant substantially diverge in layout and control hierarchy. Display and Dashboard also differ. WPF visual parity is NOT achieved.

Implemented initial cross-screen package: common light shell/white card styling, WPF-sized default window, green navigation highlight, and new Events page with prioritized expiring virtual-display event queue, cancel/clear, timed preview and playback restoration. Files: MainWindow.axaml, MainWindow.axaml.cs, MainWindow.Events.cs. Current HEAD CI and desktop checks pending. Further WPF screen-by-screen UI reconstruction and behavior parity remain open; do not claim screenshot parity complete.

## Screenshot-driven multi-screen layout package (2026-10-19, continuing)

- Source: owner's LIS2.zip with ten WPF/Avalonia screenshot pairs; WPF is authoritative reference. Current Avalonia layouts differ materially on every screen, particularly fan controls, hardware, pages, media and Home Assistant.
- Added WPF-oriented card grouping and contextual headings for Fan Control, Hardware, Media, Pages and Home Assistant; four fan-channel visual indicators are explicitly simulation-only and update from actual dry-run outputs, never misleadingly claim physical fan readings.
- Previously added light shared shell, green selection and event composer/queue; all must still pass current HEAD CI and Windows/Linux runtime acceptance. A full 10-screen 1:1 visual parity result has NOT been achieved; page editor, fan curve, HA entity table, hardware sensor table and media composition remain outstanding. Keep these as open scope; no final acceptance claim.
- Latest code commit before this status update: `8f6992b`. Await GitHub Actions for this HEAD; no local dotnet compiler in current container.

## Coherent UI follow-up: HA entity table, page preview, fan curve (2026-10-19)

- Previous HEAD `ab71feb` passed GitHub Actions #39272898902 on Windows/Linux. New HEAD CI is not yet confirmed.
- Home Assistant: entity table now shows favorite marker, entity ID, friendly name, current state and template key. Selection synchronizes with existing combo, filters, alias/favorite editor and attribute/details card; existing HA source and token handling unchanged. Changes: `MainWindow.axaml`, `MainWindow.HomeAssistant.cs`.
- Pages: fixed-size two-line VFD editor preview now matches the 402×112 preview geometry used on Dashboard. Sensor group headers use the light WPF-style palette.
- Fans: temperature-to-output curve plot alongside text editor, refreshed as points change or channel selection changes. Four output bars remain dry-run previews only. No change to serial authorization, safety frame, or physical fan commands.
- Still pending: exact WPF UI parity on all ten screens, complete language/theme translation, interactive desktop comparison and current HEAD CI. The UI work is not accepted as 1:1 parity.

## WPF UI alignment: hardware and media grouping (2026-10-19)

- Reworked Hardware page into three WPF-oriented white cards: live sensor browser with search/filters/grouped rows, selected sensor aliases/favorites/fan-channel assignments, and display presets. Existing event handlers and safety restrictions retained.
- Media Windows provider cards now have WPF-style right-aligned status badges. Winamp has distinct Playback, Playlist, Audio and VU/Spectrum groups; playlist position/count and bitrate/sample rate are bound to existing Winamp telemetry keys, not placeholder data.
- Existing Pages VFD preview and HA table, fan curve and virtual Events work from preceding package remain. UI parity is still incomplete on all ten screens, including full localization and themes. Source comparison only, no desktop screenshot acceptance yet.
- Changes: `MainWindow.axaml`, `MainWindow.Media.cs`; commits `7f457cf`, `ede7153`, `0af5658`, `d87bf60`. Latest HEAD CI not yet green; Actions concurrency cancels intermediate runs. Next: validate HEAD and fix any failures, then continue detailed WPF parity and UI tests.

## WPF UI alignment follow-up: Pages and Display (2026-10-19)

- Pages editor now groups page properties, template variable insertion and fixed-size VFD preview with clearer labels and spacing. List width and height improved; page behavior and persistence untouched.
- Display view now groups display controls, ROM, custom character builder and brightness with WPF-style headings and more room for glyph editing. Preserve Avalonia markup escape on literal glyph template keys.
- Dashboard fan status wording corrected: safety authorization remains required; do not suggest that the runtime is absent.
- HEAD before documentation: `fdf00fc`. No current HEAD CI success yet. This is source-based layout progress, not completed WPF GUI parity. Missing: full ten-screen pixel/interaction/theme/language acceptance and any failed build repairs.

## GUI parity continuation: full-page descriptions and theme resources (2026-10-19)

- Removed misleading navigation placeholder text claiming Media, Events, Diagnostics and Home Assistant are unconnected. Every page now has a concrete description reflecting existing functionality.
- All ten page descriptions translated into English (fallback), German, French, Turkish and Russian, respecting saved explicit language or OS language. Navigation update and settings language change apply translations immediately.
- Centralized card, surface, sidebar, border, and selection colors into Avalonia dynamic resources. Light/Dark/System setting updates resources and navigation colors; replaces hard-coded white backgrounds across pages. WPF light palette preserved.
- This is a substantial theme/i18n improvement, NOT a claim of 1:1 WPF GUI completion. Remaining blockers: complete translation of page-level content and buttons, precise screenshot comparison of all ten pages, accessibility and runtime smoke acceptance. Latest HEAD before this note `4b26a6c`; CI not confirmed green. Do not remove WPF project yet.

## Continued 10-page GUI parity implementation (2026-10-19)

- Added dark-aware theme resources for sidebar icons, muted cards, group headers and status indicators, replacing remaining hardcoded light-only brushes in these shared UI elements. Theme setting now updates the additional resources.
- Added reversible German translations for dozens of static labels spanning Dashboard, Display, Pages, Media, Events, Hardware, Fan Control, Diagnostics and Home Assistant; selection and live telemetry are not overwritten. Hooked into language changes. Other languages still require full page-level translations; navigation and page descriptions support five languages.
- Code: `App.axaml`, `MainWindow.axaml`, `MainWindow.Settings.cs`, new `MainWindow.UiLocalization.cs`, `MainWindow.SettingsLocalization.cs`.
- This work is NOT verified 1:1 visual parity. Remaining: screen-by-screen screenshot comparisons, exact layout/control parity, other-language labels, complete dark-mode validation, desktop runtime tests and HEAD CI. Do not remove WPF yet.

## WPF parity continuation and compile repair (2026-10-19)

- Added German translations for interactive command buttons across all ten Avalonia pages, preserving command bindings and reversible language changes. Existing static-label and page-description translation remains.
- Matched WPF Events composer 20-character limit for both LIS2 display lines and adjusted queue label/minimum height.
- Examined Actions failure #39275645016 for `a015d3ad`: compiler CS0103 and CS0234 from ambiguous/unqualified Avalonia namespace references in MainWindow, Fans and Settings. Fixed with `global::Avalonia` qualifications; HEAD CI is pending, not yet green.
- Source control inventory of WPF/Avalonia interactive controls completed for all ten tabs; this is not a visual or functional 1:1 certification. Remaining: exact WPF layout/behavior review, full translations for French/Turkish/Russian static controls, CI and final owner desktop verification. No claim of 1:1 parity yet.

## Events WPF queue parity (2026-10-19)

- Replaced single concatenated event queue strings with WPF-style three-column rows (priority name, display line text, remaining time). ItemTemplate binds typed rows and preserves selected-index cancellation, priority queue order and existing virtual display behavior.
- Compared all ten WPF/Avalonia page control counts as an inventory only; counts are not a substitute for functional/visual parity. WPF has 10 tabs, Avalonia 10 navigation pages; many have differing control counts because of platform-specific transport and fan safety controls.
- Latest code commits: `b7a395f` layout, `fca10d6` event rows. Full 1:1 acceptance remains unverified; head CI pending.

## Large WPF parity block: media and pages (2026-10-19)

- Media: Windows session card now follows WPF hierarchy (title/artist/album, hidden technical playback status, bottom timeline and right-aligned create-page button). Winamp, legacy WMP and Linux MPRIS share WPF-size typography, truncation and subdued album text; existing provider bindings untouched.
- Pages: restored WPF preset picker and create-from-preset button above the list. Clock and LIS2 Status templates are constructed with WPF's exact name, line templates, duration and truncate overflow; added to PageWorkspace and persisted. WPF contains additional hardware/media presets not yet ported; do not claim full preset parity.
- Localized new preset controls to German. Commits: `0f7ec0d`, `3d495e2`, `4764ab3`, `0275735`.
- Visual/interaction 1:1 acceptance is not complete; no claim of fully finished GUI. Continue remaining WPF presets, fan control controls and screenshot checks. Current CI not confirmed green.

## WPF page preset restoration block (2026-10-19)

- Pages preset picker now lists all 11 WPF-enabled definitions in WPF order/categories: Clock, Status, CPU, GPU, Memory, Temperatures, Fans, Winamp Now Playing, Winamp VU, Winamp Spectrum, Windows Media Now Playing. WMP Legacy presets remain disabled, matching the WPF reference.
- Ported actual Clock/Status and six? hardware/media presets through `CreateWpfPagePreset`; Winamp and WindowsMedia templates/conditions/durations/overflow/timing match WPF definitions. Hardware presets use available live Hardware.* numeric source values and fail explicitly if unavailable, rather than inventing sensor data. Hardware matching is heuristic and still needs WPF sensor-type/alias/favorite parity review.
- Preset creation saves to PageWorkspace and selects the new page. Winamp Spectrum custom glyph confirmation/programming from WPF is NOT yet implemented, so full preset parity is still incomplete.
- Inspected failed CI run #39276890834: Avalonia XAML AVLN2000 on unsupported `StrokeLineJoin` attribute in FanCurvePreviewPath. Removed unsupported attribute in `5c782e6`; current HEAD CI pending.
- No claim of 1:1 GUI acceptance: exact WPF control parity, per-screen screenshots and desktop runtime verification remain outstanding.

## WPF Pages checkbox / Fan Control layout parity (2026-10-19)

- After green Actions run #446 (all platform jobs passed), replaced the Pages list's non-WPF `(off)` text convention with a WPF-style checkbox and ellipsized page name for each row. Clicking a row checkbox updates the indexed PageWorkspace entry, refreshes the editor/list and persists the workspace. The checkbox binding is explicitly OneWay to avoid writing to immutable display records; the click handler owns persistence.
- Moved `FanAutomatic` to the Fan Outputs header on the right, matching the WPF automatic-control position; removed the duplicate header/control from the lower fan configuration card. Existing authorization/physical fan safety logic untouched.
- Commits `45a62c0`, `66c5978`, `7780fb5`, `2afd39d`. New CI not yet confirmed; WPF full GUI parity remains outstanding.

## Windows owner smoke-test fixes: Dashboard language and hardware sensors (2026-10-19)

- Owner confirmed the latest green Windows preview looks substantially closer to WPF, but Dashboard remained English and Hardware sensors were absent.
- Root cause for sensors: `WindowsHardwareFanSensors` exposed only Temperature values and emitted unprefixed keys; `PageSourceValues.UpdateHardware` requires `Hardware.` prefix. Now enumerate all finite LibreHardwareMonitor sensor types (Load, Data, Fan, Temperature, etc.) with WPF-compatible `Hardware.<hardware>.<type>.<sensor>` keys and `.Unit` entries. Existing LHM hardware discovery/refresh and read-only fan-safety behavior preserved. Runtime Windows validation remains pending.
- Dashboard: added missing German static fan names and guidance, checkbox labels, dynamic virtual-device status, and immediate status refresh on language changes. Existing reversible original-label mechanism retained. German is not forced; selected UI language remains authoritative.
- Commits `6dd2a92`, `b1a6737`, `5e771b7`, `ce84b5d`. Await CI and Windows desktop validation; do not claim all UI languages or 1:1 acceptance.

## Localization parity and runtime-text safety (2026-10-20)

- WPF `LocalizationService` translations were copied into the Avalonia `WpfTranslationCatalog` and integrated into page labels, buttons, checkboxes, combo-box entries, watermarks and tooltips. Traversal now includes logical and visual descendants; newly realized controls are localized after navigation.
- Language changes normalize already translated labels back to their English source. Avalonia-specific navigation aliases for Dashboard (German, French, Turkish, Russian) are handled separately from the WPF catalog's different Turkish/Russian wording.
- Added localization regression tests for catalog entries, language roundtrips, prefixed labels, and preservation of runtime sensor/media text. The latest change guards previously localized controls against overwriting live telemetry, media titles or status values.
- Commits include `3d144d8`, `fb6ecfc`, `1d41208`, `59d2cee`, `0033794`, `6947267`, `3f1bdf8`. CI run 475 passed at `0033794`; run 477 for latest `3f1bdf8` was still in progress when recorded. Do not claim latest HEAD is green until confirmed.
- Remaining: full ten-view WPF source/UI/behavior parity audit, all-language runtime coverage, Windows/Linux desktop sign-off, and final product acceptance. WPF remains authoritative; physical hardware is not available and is not a package gate.

## Localization lookup performance and key stability (2026-10-20)
- Reverse lookup of translated WPF strings now uses a one-time indexed catalog rather than nested scans of all language dictionaries for each control on every navigation/language switch. Existing Avalonia navigation aliases take precedence; original English WPF catalog keys are preserved before reverse lookup to prevent accidental translation collisions.
- Added tests for English-key stability and WPF translation roundtrips across German, French, Turkish and Russian. Changes: `fbf90de`, `f39fba8`. CI for this HEAD is pending; do not mark package accepted until Windows/Ubuntu jobs pass.
- This is a localization reliability/performance increment, NOT completion of the ten-view WPF parity package. The latter still requires comprehensive source audit, functionality, integration tests and final owner desktop acceptance.

## Startup transport selection guard (2026-10-20)
- Avalonia window startup now connects the virtual LIS2 and starts virtual page playback only when the selected transport is Virtual. Previously the Opened handler unconditionally connected the simulator, even if Serial had been selected. Existing explicit Apply/reconnect behavior and physical fan authorization remain unchanged.
- Commit `a5e7afc`; latest HEAD CI pending. This is a focused transport-safety correction, not completion of the full WPF ten-view parity package. Verify startup mode behavior in final desktop acceptance.

## Hardware browser: language-independent sensor filters (2026-10-20)
- Fixed Hardware sensor type filtering: the selected ComboBox item's translated label was previously passed directly to the English-only HardwareSensorCatalog.MatchesType, causing non-English UI selections to return incorrect or empty results. Filtering now maps the stable selected option index to canonical English filter keys while preserving all localized labels.
- Added parameterized tests covering all twelve filter choices and a sensor-result regression for Temperature, Load and All types. Commits `c4217f8`, `a47daec`. Current HEAD CI pending; Windows/Linux runtime and complete WPF UI parity are not yet signed off.

## Hardware templates: formatted sensor references and offline tracking (2026-10-20)
- HardwareSensorCatalog.ExtractTrackedKeys now removes template format specifiers such as `:F1` or `:0` before recording hardware keys. Previously formatted page placeholders could be tracked as invalid sensor IDs, hiding the actual disconnected sensor in the Hardware browser.
- Added regression test for mixed formatted/unformatted references, deduplication, unrelated Home Assistant placeholders, and offline/unavailable sensor entries. Commits `8f2a03a`, `f0f4b37`; HEAD CI pending. This is a bounded template/browser integration fix, not complete WPF hardware or UI parity.

## Hardware preset/catalog consistency (2026-10-20)
- Display preset generation now consumes the already validated HardwareSensorCatalog entries for availability and numeric finiteness rather than performing a separate numeric conversion. It also uses each catalog entry's unit directly, removing repeated snapshot scans and aligning preset unit semantics with the Hardware browser.
- Added tests for case-insensitive unit metadata and rejection of NaN/infinite readings. Commits `385f4b1`, `ce0cf1d`; CI for current HEAD pending. Full hardware/WPF parity and owner desktop acceptance remain open.

## Owner Windows Home Assistant acceptance and Hardware live-selection (2026-10-20)
- Owner reports that Home Assistant connection works in the Avalonia Windows application. This is a manual Windows connectivity confirmation only, not an acceptance of all HA features or Linux behavior.
- Hardware live-refresh now tracks selected sensor identity separately from the ListBox's transient SelectedItem while rebuilding grouped entries. This avoids losing the selected sensor when ItemsSource rebinding clears the selection before it can be restored. Commit `3675cec`. CI pending. Broader WPF parity and end-to-end hardware acceptance remain open.

## Avalonia-only searchable page variable browser (2026-10-20)
- Owner explicitly approved departing from WPF's giant template-variable ComboBox. Replaced it with a modal variable browser: bounded 50-result search, multi-term case-insensitive matching, category filters (All/Favorites/HA/Hardware/Clock/Media), session favorites, current-value/template preview, refresh and insertion into either line at the cursor. Existing `{key}` template syntax is preserved. Catalog search runs only on dialog/filter actions rather than rebuilding thousands of ComboBox items on each sensor tick.
- Code commits `52e29fd`, `8b11d48`, `8bbe024`; regression tests `e1966a4`. CI pending; favorites currently session-only and HA attributes are not yet grouped by entity. This is an intentionally Avalonia-specific usability deviation, not a claim of complete browser feature parity or owner desktop acceptance.

## Page variable browser completion pass (2026-10-20)
- Fixed HA categorization for actual runtime `HA.` keys (rather than only `HomeAssistant.`). Added friendly-name labels from connected HA entities, per-entity filtering that exposes state and attribute variables, multi-term search across keys and friendly labels, and persistent favorites through PortableWorkspace.PageVariableFavorites. Existing page template keys remain unchanged.
- Commits `9fe4fbd`, `fd77ab2`, `e66573a`, `c24c345`, `c0bcfcf`. Added HA entity/attribute label tests. Latest HEAD CI pending. The UI has not yet been manually accepted on Windows/Linux; do not resume WPF parity work until this browser passes CI and desktop acceptance.
