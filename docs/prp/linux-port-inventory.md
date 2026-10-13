# Linux/Avalonia port — code inventory (2026-10-13)

**Status: repository reconnaissance, not a complete exhaustive file-tree audit.** Source and project files listed below were read directly from GitHub default branch. Repository-wide recursive enumeration and execution of builds were unavailable in this session; therefore the Phase 0 *full inventory* gate remains OPEN. Do not treat this report as proof that every source file has been inspected.

## Critical findings (ordered by priority)

1. **Global Windows TFM**: `Directory.Build.props` applies `net8.0-windows10.0.19041.0` to every project, including Core/Display/Fans. `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`. **First porting change:** split shared `net8.0` from Windows-only target via project-specific properties or scoped props, retaining Windows-only API projects.
2. **Monolithic window orchestration**: `src/LIS2.App/MainWindow.xaml.cs` is **4551 lines / 160249 characters**. It directly owns `SettingsStore`, `DataSourceRegistry`, `PageScheduler`, `EventQueue`, `DisplayRuntime`, `AsyncCoalescingRunner`, `FanController`, transports, source instances, tray, startup service, and multiple WPF `DispatcherTimer` instances. Extract runtime and test it independently before UI rewrite.
3. **Windows UI + build dependencies**: `src/LIS2.App/LIS2.App.csproj` has `UseWPF=true`, `UseWindowsForms=true`, `System.Security.Cryptography.ProtectedData 8.0.0`, and a pre-build `powershell.exe` icon-generation target. A cross-platform project cannot inherit these assumptions.
4. **Windows-specific providers**: `src/LIS2.WindowsMedia/WindowsMediaDataSource.cs` imports `Windows.Media.Control` and `Windows.Media.Playback` and uses GSMTC session events; keep as Windows-only provider. `src/LIS2.Sources/LibreHardwareMonitorDataSource.cs` uses `LibreHardwareMonitor.Hardware.Computer` with CPU/GPU/memory/motherboard/storage/network/controller monitoring, polling every second. Replace behind a provider interface on Linux.
5. **Existing fan safety**: `docs/architecture.md` requires serialized commands and post-reconnect ordering: safe fans, brightness, glyphs, frame, normal fan regulation. This is a release gate, not optional UI polish.
6. **Winamp interoperability**: `src/LIS2.Winamp/WinampPipeServer.cs` uses `NamedPipeServerStream` and newline-delimited UTF-8 JSON. The C# snapshot parser is reusable; native `native/LIS2.WinampPlugin` is a Win32/x86 plugin and cannot simply be built as Linux-native Winamp support. Wine transport requires its own proof-of-concept. `docs/winamp-integration.md` confirms 20-band spectrum and VU via native Winamp APIs, not provided by MPRIS.
7. **Settings and secrets**: `src/LIS2.App/SettingsStore.cs` writes JSON to `Environment.SpecialFolder.LocalApplicationData/LIS2ControlCenter/settings.json`, supports explicit path and invalid-JSON quarantine. `src/LIS2.App/AppSettings.cs` holds transport mode, port, brightness, theme, language, pages, custom glyphs, hardware preferences, HA and fan settings; page refresh interval clamped to 100–60000 ms, scroll step 50–10000 ms. Preserve settings schema and migrate secret protection explicitly.
8. **OS shell**: `src/LIS2.App/StartupService.cs` uses Windows HKCU Run key and published exe path; `TrayIconService.cs` uses `System.Windows.Forms.NotifyIcon` and WPF media; `ThemeService.cs` uses Windows registry, WPF resource dictionaries and `dwmapi.dll`; `StartupSplash.cs` uses a dedicated STA WPF dispatcher/thread. These require platform abstractions and UI reimplementation.
9. **Protocol tester is also WPF**: `tools/LIS2.ProtocolTester/LIS2.ProtocolTester.csproj` sets `UseWPF=true` and `OutputType=WinExe`. Do not list it as automatically Linux-ready; extract protocol test logic or add a Linux CLI/Avalonia tool.
10. **CI/release Windows-only**: `.github/workflows/build.yml` and `.github/workflows/release.yml` use `windows-latest`, Windows path syntax, WPF smoke tests, native Win32 plugin, win-x64 publish. Preserve Windows jobs; add separate Linux test/publish jobs rather than retargeting the existing solution indiscriminately.

## Verified project dependency map

| Project | Direct references / package evidence | Proposed treatment |
| --- | --- | --- |
| `LIS2.Core` | `System.IO.Ports 8.0.0`; `SerialLis2Transport` uses 19200/8N1 | Retarget shared net8.0 after API audit; Linux serial tests |
| `LIS2.Display` | references Core | Shared net8.0; byte-for-byte frame tests |
| `LIS2.Sources` | `LibreHardwareMonitorLib 0.9.6`; HA WebSocket source | Split shared registry/HA from Windows sensor implementation |
| `LIS2.Fans` | no package reference in csproj | Shared net8.0; retain fail-safe tests |
| `LIS2.Winamp` | references Sources; NamedPipeServerStream | Windows integration retained; move generic media snapshot model if useful |
| `LIS2.WindowsMedia` | references Sources; GSMTC implementation | Windows-only |
| `LIS2.WmpLegacy` | references Sources; feature deferred | Keep Windows-only/deferred |
| `LIS2.App` | Core, Display, Sources, Fans, Winamp, WindowsMedia, WmpLegacy; WPF/WinForms/DPAPI | Retain WPF Windows app while extracting runtime and adding Avalonia |
| `LIS2.ProtocolTester` | Core, System.IO.Ports; WPF | Separate Linux tool strategy |
| `LIS2.WinampSimulator` | Winamp library; console executable | Test IPC portability independently |

The solution also includes `LIS2.WmpLegacySimulator` and seven named test projects: Core, Display, Sources, Fans, Winamp, WmpLegacy, WindowsMedia, App. Their implementation files still require exhaustive review.

## Verified implementation entry points

- `src/LIS2.App/App.xaml.cs`: WPF startup, splash, `Microsoft.Win32.SystemEvents.UserPreferenceChanged`, theme and localization bootstrap.
- `src/LIS2.App/MainWindow.xaml`: 1320×900 WPF window (minimum 1120×760), left navigation/sidebar, resource styles. Full view/control census remains open.
- `src/LIS2.App/MainWindow.xaml.cs`: all major orchestration and timers mixed with UI; extract first.
- `src/LIS2.App/SettingsStore.cs`, `AppSettings.cs`: settings model and persistence.
- `src/LIS2.App/StartupService.cs`, `TrayIconService.cs`, `ThemeService.cs`, `StartupSplash.cs`, `GenerateAppIcon.ps1`: Windows UI/system integration.
- `src/LIS2.Core/SerialLis2Transport.cs`: synchronous `SerialPort.Write` behind task-returning API; verify cancellation and disconnect semantics on Linux.
- `src/LIS2.Sources/LibreHardwareMonitorDataSource.cs`: 1s `PeriodicTimer`, sensor keys derived from normalized hardware/sensor names; Linux backend key compatibility is nontrivial.
- `src/LIS2.Sources/HomeAssistantDataSource.cs`: `ClientWebSocket`, async lifecycle lock, reconnect loop, immutable snapshots; good candidate for shared net8.0.
- `src/LIS2.WindowsMedia/WindowsMediaDataSource.cs`: Windows media session manager, media/timeline callbacks.
- `src/LIS2.Winamp/WinampPipeServer.cs`: JSON-line pipe receiver and snapshot converter.

## Existing user-facing feature inventory (documented in README/TODO; needs per-control audit)
- Display: serial/virtual transport, 20×2 VFD, brightness, diff writes, ROM map, eight custom glyphs and CG Builder.
- Pages: templates, per-page refresh/duration, scheduler, PingPong/Marquee/Truncate, conditions, formatting, overlays and presets.
- Sensors/fans: browser, aliases, favorites, missing sensors, four fan outputs, Fixed/Curve/Follow/External/Off, hysteresis/fail-safe.
- Media: Windows Winamp metadata/VU/spectrum, modern WMP via GSMTC; WMP Legacy disabled.
- HA: WebSocket `get_states` + `state_changed`, entities/attributes, aliases, favorites, read-only.
- UI: tray, autostart, splash, Light/Dark/System, six languages, context help, diagnostics/export.
- CI: Windows build/tests/WPF smoke and screenshot regression; win-x64 artifacts and native x86 plugin.

## Concrete follow-up audit checklist (DO NOT check until done)
- [ ] Obtain recursive, SHA-pinned repository tree; list every source, XAML, CMake, workflow, test and resource file with size.
- [ ] Read *all* source files, including remaining partial `MainWindow` and XAML; create view-to-service call graph.
- [ ] Inventory every OS-specific import/API, native interop, Windows target/package and build target across the tree.
- [ ] Map every UI event handler, timer, service lifecycle, error path and startup/shutdown dependency.
- [ ] Audit every test fixture and assertions for Windows-only assumptions; establish baseline pass/fail and CI commit SHA.
- [ ] Inspect `native/` and all CMake targets, message schema and Wine constraints.
- [ ] Inspect complete `TODO.md`, protocol unknowns and outstanding physical-device issues before feature parity claims.
- [ ] Produce an exhaustive file-level migration matrix with change owner, dependencies and acceptance tests.
- [ ] Execute platform-neutral library builds and tests on Linux only after code changes are authorized.

## Architecture decisions to carry into implementation
1. Introduce `LIS2.Runtime` as UI-neutral orchestrator and keep WPF shell working.
2. Remove global Windows TFM from portable projects; explicitly target Windows for Windows-only projects.
3. Split shared provider contracts/HA from Windows LibreHardwareMonitor.
4. Provide OS media providers (Winamp/GSMTC on Windows; MPRIS on Linux), without promising spectrum parity.
5. Port to Avalonia incrementally; preserve Windows baseline and fan safety at each gate.
6. Treat Wine Winamp and macOS as optional, separate validation tracks.

**No porting work performed in this inventory.**
