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

## Verification and limitations
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
