# Inventar des Avalonia-Branches

Stand: 20.10.2026. Implementierungsstand: PORT-02.
Grundlage: aktueller Git-Arbeitsbaum, Projektdateien, Quelltexte und Workflow-Konfiguration. Kein ZIP-Archiv als Implementierungsbaseline.

## Umfang und Projekte

Der Quellbestand umfasst 291 Dateien, darunter 224 C#-Dateien und 22 C#-Projektdateien. Es gibt neun Testprojekte. Das Avalonia-Testprojekt enthält 37 `*Tests.cs`-Dateien. Dateizahlen sind keine Testfall- oder Coverage-Angabe.

| Gruppe | Projekte | Target |
| --- | --- | --- |
| Portable Bibliotheken | Core, Display, Fans, Sources, Winamp, WmpLegacy, Runtime | `net8.0` |
| Gemeinsame Desktop-App | App.Avalonia | `net8.0`, Avalonia 11.3.6 |
| Windows-Referenz und Anbieter | App, WindowsMedia | Geerbtes `net8.0-windows10.0.19041.0` |
| Portable Testprojekte | Core.Tests, Display.Tests, Fans.Tests, App.Avalonia.Tests | `net8.0` |
| Windows-Testprojekte | App.Tests, Sources.Tests, Winamp.Tests, WindowsMedia.Tests, WmpLegacy.Tests | Geerbtes Windows-Target |
| Tools | ProtocolTester, WinampSimulator, WmpLegacySimulator | Geerbtes Windows-Target |

`Directory.Build.props` setzt Nullable, implizite Usings, Warnings-as-errors und das Windows-Target. Portable Projekte überschreiben ihr Target ausdrücklich. `LIS2.Sources` und `LIS2.App.Avalonia` referenzieren LibreHardwareMonitorLib 0.9.6. Der direkte Windows-Provider bleibt hinter einer OS-Auswahl; Paketabhängigkeit allein ist kein Linux-Runtime-Nachweis.

Native Projekte: `native/LIS2.WinampPlugin` (x86), `native/LIS2.WmpLegacyPlugin` und `native/LIS2.WmpLegacyVisualization`. Die Avalonia-Branch-CI baut keine nativen Plug-ins.

## Quellbereiche

| Bereich | Einstiegspunkte und Verantwortung |
| --- | --- |
| Protokoll | `src/LIS2.Core/Lis2Protocol.cs`, `Lis2Device.cs`, `SerialLis2Transport.cs`, `VirtualLis2Transport.cs` |
| Displaymodelle | `src/LIS2.Display`: Scheduler, Scroller, Templates, Sichtbarkeit, EventQueue und FrameWriter |
| Runtime | `src/LIS2.Runtime`: SourceRuntime, AdaptiveRuntimeLoop, FanRuntime/FanRuntimeLoop, PlatformContracts und Linux-Sensoren |
| Verlinkte Runtime-Dateien | `DisplayRuntimeHost`, `PagePlayback`, `PageDefinition`, `PageFrameDispatcher`, `PageSourceValues`, `PageClockValues`, `PeriodicRuntimeLoop`, `PageSequenceController` liegen unter `src/LIS2.App.Avalonia` und werden von Runtime kompiliert |
| Avalonia-Shell | `App.axaml(.cs)`, `Program.cs`, `MainWindow.axaml(.cs)`; zehn Views und partielle Handlerdateien |
| Sessions | IDisplaySession, VirtualDeviceSession, SerialDisplaySession, UnifiedLis2Session, AuthorizedFanRuntimeSession und SafeSerialFanSession; MainWindow.DeviceActions/DashboardFans für die gemeinsame Bedienung |
| Workspace | PortableWorkspaceStore, PortableFanSettings, WindowsSettingsImporter, MainWindow.Workspace/WindowsImport; PageWorkspaceActions verbindet Quellenaktionen, Editor, Sequenz-Reload und Persistenz |
| Geheimnisse | PortableHaTokenStore, LegacyHaTokenStore; DPAPI, secret-tool und lokaler AES-Dateipfad |
| Hardware | LinuxSystemSensors/LinuxHwmonFanSensors, WindowsHardwareFanSensors, HardwareSensorCatalog und HardwareDisplayPresetBuilder |
| Medien | MainWindow.Media, WindowsMediaBridge und LinuxMprisSource; Originalquellen in Winamp, WindowsMedia und WmpLegacy |
| Hilfe/Sprachen | HelpReferenceCatalog, MainWindow.HelpAbout, WpfTranslationCatalog, Settings-/UI-/Browser-Lokalisierung |

Die `IPlatform*`-Verträge haben derzeit keine entsprechenden Adapterimplementierungen. UI-unabhängige Schleifen bestehen neben weiterer Orchestrierung in den MainWindow-Dateien.

## Timer und Lebenszyklus

| Lauf | Implementierung / Takt | Beenden |
| --- | --- | --- |
| Virtuelle/serielle Seiten | Je AdaptiveRuntimeLoop, vom nächsten Scheduler-/Scrolltermin bestimmt | StopAndDrainAsync plus Sequenz-Drain |
| Editorpreview | DispatcherTimer, initial 250 ms, danach rendererabhängig mit Untergrenze 20 ms | Navigation pausiert; Closed stoppt |
| Hardware | DispatcherTimer, 2 s | Closed stoppt |
| Medien | DispatcherTimer, 1 s; Linux-Abfrage ohne Überlappung | StopMediaAsync stoppt Timer/Anbieter |
| Ereignisanzeige | DispatcherTimer, 250 ms | Closed stoppt |
| HA-Anzeige | PeriodicRuntimeLoop, 2 s | Stop plus StopAndDrainAsync |
| Lüfterregelung / Dry-run | FanRuntimeLoop bzw. PeriodicTimer, 2 s | Stop/Drain, Freigabeentzug und angeforderter Sicherheitsframe |

`MainWindow.OnWindowClosed` enthält das asynchrone Aufräumen. Windows-Schließen blendet bei Traybetrieb zunächst nur das Fenster aus. Der Prozessende-Nachweis durch einen Avalonia-GUI-Smoke fehlt.

## Persistierte Daten und Grenzen

`PortableWorkspace` enthält TransportMode, PortName, HomeAssistantEnabled, Pages, Fans, HomeAssistantUrl, CustomGlyphs, HomeAssistantEntityPreferences, HardwareSensorPreferences, PageVariableFavorites, BrightnessPercent, ProgramCustomGlyphsOnConnect, ThemeMode und LanguageMode.

Der Windows-Importer übernimmt Transportmodus/Port, Seiten, Glyphen, HA-Aktivierung/-URL/-Präferenzen, Hardwarepräferenzen, Fans, Helligkeit, Glyph-on-connect, Theme und Sprache; Windows-Tokens und andere unbekannte Felder werden nicht übertragen. Der Importbutton ist in den Einstellungen sichtbar. Ein fehlender gespeicherter Port bleibt in der Auswahl erhalten. Helligkeit und konfigurierte Glyphen werden beim Verbinden angewendet.

## Testumfang und CI

`linux-portable.yml` führt je Windows/Ubuntu vier Testprojekte aus: Core, Display, Fans und App.Avalonia. Runtime-, Source-/Media-Adapter-, Workspace-, Tokenstore-, Session- und HelpReferenceCatalog-Tests liegen im Avalonia-Testprojekt.

Nicht separat in diesem Branch-Workflow ausgeführt werden Sources.Tests, Winamp.Tests, WindowsMedia.Tests, WmpLegacy.Tests und App.Tests. Diese gehören zum Windows-Solution-Workflow.

Avalonia-Testgruppen:

- Scheduler/Scrolling/Preview, Frames, Events, Reload und Parallelität.
- Quellen-Snapshots, HA-Schlüssel, Medienparser/Invalidierung und Sprachkataloge.
- Sensoren, Presets, fehlende Werte und Fan-Fail-safe.
- Virtuelle/serielle Session, gemeinsame Ausgabe, Freigabe und Fehlereinspeisung.
- Workspace, Windows-Import, Glyphendateien, Tokenstore und Hilfeinhalte.
- Gemeinsame Quellenaktionen mit gültigen/ungültigen Entwürfen, beiden Sequenzen und persistierter leerer Seitenliste.

`DeviceUiTests` startet zusätzlich das tatsächliche Fenster mit isolierten Stores und simuliertem seriellen Transport über Avalonia.Headless.XUnit und Skia. Geprüft werden Geräte-Buttons, Einstellungen/Import, manuelle Lüfter, Transportwechsel und Trennen per Tastatur. Dashboard, Display und Settings werden in Normal-/Mindestgröße gerendert. Hintergrundquellen sind in dieser Fixture ausdrücklich abgeschaltet. Paketstart, echter Desktop-/Tray-Lebenszyklus, DBus-/HA-/Player-/Sensorintegration und physische LIS2-Ausgabe sind damit nicht abgedeckt. Aktueller CI-Nachweis und Grenzen stehen im [Status](linux-port-status.md).

## Referenzen

Die WPF-Referenz bleibt in `src/LIS2.App`, einschließlich MainWindow, UI-Regression, HelpWindow, AboutWindow, Themes, SettingsStore und Plattformdiensten. Das [Paritätsaudit](wpf-avalonia-parity-audit.md) enthält das quellengeprüfte Inventar der 126 benannten WPF-Tab-Controls und 71 tabbezogenen XAML-Handlerverwendungen sowie den aktuellen Avalonia-Abgleich.

Protokollunsicherheiten werden ausschließlich in [protocol.md](../protocol.md) und [reverse-engineering.md](../reverse-engineering.md) gepflegt. Nächste Implementierungsschritte stehen in [TODO](../../TODO.md).
