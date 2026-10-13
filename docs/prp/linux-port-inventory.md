# Linux/Avalonia port — code inventory (2026-10-13)

**Status: complete repository archive inventory and static API/dependency scan (2026-10-13).** Input: user-supplied GitHub main ZIP, 172 files / 113 C# files / 19 csproj / approximately 24,020 text lines. The archive contains no .git metadata, so the exact source commit SHA cannot be independently established from the ZIP. Builds and physical hardware tests were NOT executed. See detailed audit below.

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

The solution also includes `LIS2.WmpLegacySimulator` and eight named test projects: Core, Display, Sources, Fans, Winamp, WmpLegacy, WindowsMedia, App. Their implementation files still require exhaustive review.

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

## Archive-wide static audit and remaining validation
- [x] Enumerate complete uploaded repository archive recursively: 172 files, 113 .cs, 19 .csproj, 9 .xaml, 3 .cpp, 3 .def, 2 workflows, 1 .props, 1 solution. The ZIP is not SHA-pinned; record a Git commit for later exact baseline.
- [x] Scan all source and configuration files for framework dependencies and Windows APIs; identify major view/service orchestration entry points. Full per-handler semantic review and call graph still pending.
- [x] Search all .cs/.cpp/.h/.xaml/.csproj/.props/.yml files for WPF, WinForms, registry, WinRT, P/Invoke, DPAPI, PowerShell, named pipes, serial and timers. Heuristic scan is not a compiler/platform analyzer.
- [ ] Audit every UI event handler and error path semantically. Six DispatcherTimers identified in MainWindow (pages 5s, fans 2s, hardware 1s, Winamp 1s, event UI 1s, page preview 100ms).
- [ ] Audit every test fixture and assertions for Windows-only assumptions; establish baseline pass/fail and CI commit SHA.
- [x] Inventory native projects: WinampPlugin (plugin.cpp, winamp_sdk_min.h, .def, CMakeLists); WmpLegacyPlugin (plugin.cpp, .def, CMakeLists); WmpLegacyVisualization (visualization.cpp, .def, CMakeLists). Wine compatibility still unverified.
- [x] Review remaining TODO headings and open hardware issues: protocol trailing 00, brightness/off, 8 custom glyphs, unknown device detection, fan safe minimum, real HA/sensor stability, physical hardware verification.
- [ ] Produce an exhaustive file-level migration matrix with change owner, dependencies and acceptance tests.
- [ ] Execute platform-neutral library builds and tests on Linux only after code changes are authorized.

## Architecture decisions to carry into implementation
1. Introduce `LIS2.Runtime` as UI-neutral orchestrator and keep WPF shell working.
2. Remove global Windows TFM from portable projects; explicitly target Windows for Windows-only projects.
3. Split shared provider contracts/HA from Windows LibreHardwareMonitor.
4. Provide OS media providers (Winamp/GSMTC on Windows; MPRIS on Linux), without promising spectrum parity.
5. Port to Avalonia incrementally; preserve Windows baseline and fan safety at each gate.
6. Treat Wine Winamp and macOS as optional, separate validation tracks.

## Additional verified findings from full ZIP
- **Exact file counts**: 172 regular files, including 113 C# sources, 19 C# projects, 9 XAML, 14 Markdown, 3 C++, 3 .def, 3 CMakeLists.txt, 2 GitHub Actions YAML, 1 .props, 1 .sln, 1 PowerShell script, 1 header, .gitignore and LICENSE.
- **Eight test projects**, not seven. Test source files: Core (Lis2ProtocolTests, Lis2DeviceTests); Display (PingPongScroller, AsyncCoalescingRunner, EventQueue, DisplayFrameWriter, DisplayTests, VisibilityEvaluator, TemplateRenderer, DisplayRuntime); Sources (HomeAssistantValueConverter, DataSourceRegistry); Fans (FanController); Winamp (WinampMapping); WindowsMedia (WindowsMediaValues); WmpLegacy (WmpLegacyMapping); App (VirtualLis2EndToEnd, SettingsStore).
- **Largest migration hotspots**: `MainWindow.xaml.cs` ~160 KB / 4550 newline-separated lines; `MainWindow.xaml` ~159 KB / 2307 lines; `LocalizationService.cs` ~106 KB / 1259 lines; `Themes/Common.xaml` ~32 KB / 562 lines; `HelpWindow.xaml.cs` ~26 KB / 506 lines. File line counts can differ by one with trailing newline.
- **UI navigation**: Dashboard, Display, Pages, Media, Events, Hardware, Fan Control, Settings, Diagnostics, Home Assistant (10 sections). Main window size 1320x900, minimum 1120x760. Port main navigation, 10 sections, About and Help dialogs, glyph builder, theme dictionaries and screenshot test harness.
- **UI regression matrix**: `MainWindow.UiTests.cs` explicitly iterates 2 themes (Light/Dark) x 5 languages (English, German, French, Turkish, Russian) x main tabs, captures screenshots and checks layouts. System language/theme are modes, not additional translation languages.
- **Fan runtime**: six WPF timers, including fan update every 2 seconds; current `FanController` itself is a calculation service (`CalculateOutput`, curve, clamping) rather than a background scheduler. Move timer orchestration out of UI, not the controller's math.
- **Media architecture**: `WindowsMediaDataSource` uses WinRT GSMTC and session callbacks; Winamp and WMP Legacy each use named pipes and snapshot/value classes. Linux MPRIS provider should integrate via `IDataSource` and `DataSourceRegistry`.
- **Security**: `HomeAssistantTokenProtector.cs` uses DPAPI CurrentUser with prefix `dpapi:v1:`; cannot decrypt those ciphertexts on Linux. Explicit re-entry or export/import strategy is needed; never silently discard or store tokens in cleartext.
- **OS integration**: Windows registry Run autostart, registry/DWM theme selection, WPF/WinForms tray, Windows file dialogs, Clipboard, splash STA dispatcher, Windows-only icon generation via powershell.exe, and serial port discovery using `SerialPort.GetPortNames()`.
- **App settings**: JSON LocalApplicationData settings.json, pages, glyphs, hardware preferences, HA, fan channels; existing refresh interval per page is 100–60000 ms. Preserve semantics in Avalonia.
- **Packaging**: Windows release ZIPs include app, ProtocolTester and native x86 Winamp DLL; two Windows Actions workflows. Add Linux matrix without breaking Windows artifacts.
- **No runtime verification**: .NET SDK availability and Linux compatibility of packages were not established by this archive-only scan.

## Exact migration file groups (all files in each group must be considered)
- **Shared protocol (10 C# files)**: `src/LIS2.Core/`: Lis2Protocol, Lis2Device, ILis2Device, ILis2Transport, SerialLis2Transport, VirtualLis2Transport, VirtualLis2State, VirtualLis2ProtocolInterpreter, CustomCharacterManager, Lis2Brightness. Keep protocol/virtual logic, validate serial OS behavior.
- **Display (12 C# files)**: `src/LIS2.Display/`: AsyncCoalescingRunner, DisplayEvent, PageScheduler, DisplayFrame, TemplateRenderer, DisplayOverflowMode, PingPongScroller, DisplayPage, DisplayRuntime, DisplayFrameWriter, EventQueue, VisibilityEvaluator. Retarget net8.0, preserve template, glyph and rendering semantics.
- **Sources (9 C# files)**: `src/LIS2.Sources/`: HomeAssistantDataSource, SensorRegistry, ClockDataSource, HomeAssistantValueConverter, HomeAssistantEntityInfo, LibreHardwareMonitorDataSource, DataSourceRegistry, SensorValue, IDataSource. Separate Windows LibreHardwareMonitor and Linux hwmon backend from shared contracts and HA.
- **Fans (4 C# files)**: `src/LIS2.Fans/`: FanController, FanChannelConfiguration, FanMode, FanCurvePoint. Keep shared calculations and safety tests.
- **Windows media (4 C# files)**: `src/LIS2.WindowsMedia/`: WindowsMediaValues, WindowsMediaPlaybackState, WindowsMediaSnapshot, WindowsMediaDataSource. Keep Windows-only, optionally extract snapshot model.
- **Winamp (8 C# files)**: `src/LIS2.Winamp/`: WinampPipeServer, WinampMessage, WinampValues, WinampPlaybackState, WinampDataSource, WinampSnapshot, AssemblyInfo. Preserve Windows IPC and value keys.
- **WMP Legacy (9 C# files)**: `src/LIS2.WmpLegacy/`: WmpLegacyVisualizationMessage, WmpLegacyVisualizationPipeServer, WmpLegacyDataSource, WmpLegacyMessage, WmpLegacySnapshot, WmpLegacyPipeServer, WmpLegacyValues, WmpLegacyPlaybackState, AssemblyInfo. Deferred Windows-only.
- **WPF app (35+ C# and XAML/resource files)**: `src/LIS2.App/`: MainWindow.xaml/.xaml.cs/.UiTests.cs, App.xaml/.xaml.cs, AboutWindow.xaml/.xaml.cs, HelpWindow.xaml/.xaml.cs, UiTestSupport.cs, HelpLocalization(.de/.fr/.ru/.tr).cs, LocalizationService.cs, Themes/Common/Light/Dark.xaml, SettingsStore.cs, AppSettings.cs, HomeAssistantTokenProtector.cs, ThemeService.cs, StartupService.cs, StartupSplash.cs, TrayIconService.cs, GenerateAppIcon.ps1, and row/settings/preset/glyph model files. Reuse models after moving to shared assemblies; rewrite view code and OS shell.
- **Tools**: `tools/LIS2.ProtocolTester/` (App.xaml/.xaml.cs, MainWindow.xaml/.xaml.cs, csproj), `tools/LIS2.WinampSimulator/` (Program.cs, csproj), `tools/LIS2.WmpLegacySimulator/` (Program.cs, csproj). ProtocolTester is WPF; simulators are console apps but depend on Windows IPC libraries.
- **Native**: `native/LIS2.WinampPlugin/`, `native/LIS2.WmpLegacyPlugin/`, `native/LIS2.WmpLegacyVisualization/`: all three CMake/native source sets; keep Windows-only.
- **Tests**: `tests/LIS2.Core.Tests/`, `LIS2.Display.Tests/`, `LIS2.Sources.Tests/`, `LIS2.Fans.Tests/`, `LIS2.Winamp.Tests/`, `LIS2.WindowsMedia.Tests/`, `LIS2.WmpLegacy.Tests/`, `LIS2.App.Tests/`. Extract portable test subsets and preserve Windows UI-specific suite.

**No application source, project or workflow was changed. Static inventory is complete for the uploaded ZIP; execution, full semantic review and physical validation remain open.**
