# Inventar des Avalonia-Branches

Stand: 22.10.2026. Implementierungsstand: PORT-08.
Grundlage: aktueller Git-Arbeitsbaum, Projektdateien, Quelltexte und Workflow-Konfiguration. Kein ZIP-Archiv als Implementierungsbaseline.

## Umfang und Projekte

Der mit `rg --files` ermittelte Quellbestand unter `src`, `tests`, `tools` und `native` umfasst 281 Dateien, darunter 228 C#-Dateien und 20 C#-Projektdateien. Es gibt acht Testprojekte. Dateizahlen ändern sich mit den gepflegten Quell- und Testdateien. Dateizahlen sind keine Testfall- oder Coverage-Angabe.

| Gruppe | Projekte | Target |
| --- | --- | --- |
| Portable Bibliotheken | Core, Display, Fans, Sources, Winamp, WmpLegacy, Runtime | `net10.0` |
| Gemeinsame Desktop-App | App.Avalonia | `net10.0`, Avalonia 11.3.22 |
| Windows-Anbieter | WindowsMedia | Geerbtes `net10.0-windows10.0.19041.0` |
| Portable Testprojekte | Core.Tests, Display.Tests, Fans.Tests, App.Avalonia.Tests | `net10.0` |
| Windows-Testprojekte | Sources.Tests, Winamp.Tests, WindowsMedia.Tests, WmpLegacy.Tests | Geerbtes Windows-Target |
| Portables Tool | ProtocolTester | `net10.0`, Avalonia 11.3.22 |
| Windows-Simulatoren | WinampSimulator, WmpLegacySimulator | Geerbtes Windows-Target |

`Directory.Build.props` setzt Nullable, implizite Usings, Warnings-as-errors und das Windows-Target. Portable Projekte überschreiben ihr Target ausdrücklich. `LIS2.Sources` und `LIS2.App.Avalonia` referenzieren LibreHardwareMonitorLib 0.9.6. Der direkte Windows-Provider bleibt hinter einer OS-Auswahl; Paketabhängigkeit allein ist kein Linux-Runtime-Nachweis.

Native Projekte: `native/LIS2.WinampPlugin` (x86), `native/LIS2.WmpLegacyPlugin` und `native/LIS2.WmpLegacyVisualization`. Die Avalonia-Branch-CI baut und veröffentlicht das Winamp-x86-Plug-in; beide Legacy-Plug-ins werden für x86/x64 als gemeinsame Pakete gebaut und geprüft.

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
| Geheimnisse | PortableHaTokenStore, LegacyHaTokenStore; DPAPI, secret-tool und ausdrücklich gewählter lokaler Linux-AES-Dateipfad |
| Hardware | LinuxSystemSensors/LinuxHwmonFanSensors, WindowsHardwareFanSensors, HardwareSensorCatalog und HardwareDisplayPresetBuilder |
| Medien | MainWindow.Media, WindowsMediaBridge, WindowsMediaSmoke und LinuxMprisSource; aktive Windows-Quellen Winamp/WindowsMedia/WmpLegacy; gespeicherte Linux-Playerauswahl und Spielposition; Legacy mit separater Audiopipe |
| Hilfe/Sprachen | HelpReferenceCatalog mit eigenem HelpReferenceLocalization-Katalog für fünf Sprachen, MainWindow.HelpAbout, UiTranslations/UiMessage, UiTranslationCatalog, Settings-/UI-/Browser-Lokalisierung und sprachabhängige Statusmeldungen |

Die `IPlatform*`-Verträge haben derzeit keine entsprechenden Adapterimplementierungen. UI-unabhängige Schleifen bestehen neben weiterer Orchestrierung in den MainWindow-Dateien.

### Abgleich der Plattformverträge

| Vertrag | Tatsächlicher Aufrufpfad | Abweichung vom Ziel |
| --- | --- | --- |
| `IPlatformSerialPortDiscovery` | Statische `SerialPortDiscovery` aus Settings/Serial; kein Hardwarezugriff bei Suche | Kein injizierter Vertragsadapter |
| `IPlatformSensorProvider` | `IFanSensorSource`, `LinuxSystemSensors`, `WindowsHardwareFanSensors` | Produktivvertrag liefert `object?`, Entwurf nur `double?`; vor Adapterumbau Schlüssel und Metadaten erhalten |
| `IPlatformMediaProvider` | `WindowsMediaBridge.Values`, Winamp-Values, asynchrones `LinuxMprisSource.ReadSnapshotAsync` | Synchrones Entwurfs-Snapshot bildet Start, Abbruch und Dispose nicht ab; `AsyncProviderLifetime` ordnet aktuell den Medienabbau |
| `IPlatformTrayService` | Windows-/Linux-Avalonia-Tray direkt in `App`; Exit wartet auf `BeginShutdownAsync`; Texte folgen der Sprache | Native Tray-Host-Bedienung offen; Linux schließt die App und behält minimierte Fenster in der Taskleiste |
| `IPlatformAutostartService` | Windows-Registry in `MainWindow.Settings`, Linux-XDG-Datei über `DesktopAutostart`, beide mit `--minimized` | Dateivertrag isoliert geprüft; Anmeldung auf Zielsystemen offen |
| `IPlatformSecretStore` | `PortableHaTokenStore`, DPAPI/secret-tool und ausdrücklich gewählte Linux-AES-Dateien | Spezialisierter synchroner Tokenstore statt allgemeinem Schlüsselvertrag; UI-Speichern/-Löschen läuft außerhalb des UI-Threads, initiales Laden synchron |
| `IPlatformAppearance` | Avalonia `RequestedThemeVariant`/`ActualThemeVariant`, Windows-Titelbalken; System-Themeänderungen aktualisieren die Farbressourcen | Fünf Sprachen mit drei Themes geprüft; tatsächlicher OS-Themewechsel und Monitorwechsel bleiben Zielsystemprüfung |

Der Abgleich ist eine Bestandsaufnahme. Die unbenutzten Entwurfsverträge sind kein Nachweis einer durchgängigen Plattformtrennung. Ein Umbau muss die tatsächlich benötigten asynchronen Lebenszyklen berücksichtigen; zusätzliche Linux-Desktopfunktionen gehören nicht zu PORT-03.

## Timer und Lebenszyklus

| Lauf | Implementierung / Takt | Beenden |
| --- | --- | --- |
| Virtuelle/serielle Seiten | Je AdaptiveRuntimeLoop, vom nächsten Scheduler-/Scrolltermin bestimmt | StopAndDrainAsync plus Sequenz-Drain |
| Editorpreview | DispatcherTimer, initial 250 ms, danach rendererabhängig mit Untergrenze 20 ms | Navigation pausiert; Shutdown stoppt vor Quellen-Dispose |
| Hardware | DispatcherTimer, 2 s | Shutdown stoppt |
| Medien | DispatcherTimer, 1 s; Linux-Abfrage ohne Überlappung | Timer stoppen, MPRIS abbrechen/Kindprozess beenden, Start und Refresh drainieren, alle Anbieter abbauen |
| Ereignisanzeige | DispatcherTimer, 250 ms | Shutdown stoppt |
| HA-Anzeige | PeriodicRuntimeLoop, 2 s | Stop plus StopAndDrainAsync |
| Lüfterregelung / Dry-run | FanRuntimeLoop bzw. PeriodicTimer, 2 s | Stop/Drain, Freigabeentzug und angeforderter Sicherheitsframe |

`MainWindow.BeginShutdownAsync` besitzt einen wiederverwendeten Shutdown-Task. `Closing` hält das Fenster bis zum Drain offen; Windows-Schließen blendet bei Traybetrieb nur aus. Tray-Exit und Desktop-Shutdown warten auf den Abbau, Fehler führen beim expliziten Exit zu Exit-Code 1. `RuntimeShutdown` führt trotz einzelner Fehler die übrigen Aufräumschritte aus. `--desktop-smoke <Verzeichnis>` startet isolierte Stores und das virtuelle Gerät, navigiert durch zehn Views, öffnet Hilfe/About, prüft einen fehlenden Medienanbieter und beendet den echten Desktopprozess. Physische Geräte und Livequellen werden dabei nicht gestartet.

## Persistierte Daten und Grenzen

`PortableWorkspace` enthält TransportMode, PortName, HomeAssistantEnabled, HomeAssistantLocalTokenStorage, Pages, Fans, HomeAssistantUrl, CustomGlyphs, HomeAssistantEntityPreferences, HardwareSensorPreferences, PageVariableFavorites, BrightnessPercent, ProgramCustomGlyphsOnConnect, ThemeMode, LanguageMode und LinuxMediaPlayer. Die lokale Tokenwahl greift nur unter Linux. Der Tokenwert selbst bleibt außerhalb des Workspace.

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

`MenuLanguageUiTests` prüft die zehn Menüseiten und About in fünf Sprachen sowie Systemstandard, Meldungen beim Sprachwechsel, unveränderte Nutzdaten und das Speichern übersetzter Lüftermodi. `MenuTranslationCatalogTests` prüft statische AXAML-Texte einschließlich Tooltips/Platzhaltern, Formatargumente und verschachtelte Meldungen. `UiTranslations` ergänzt die vorhandenen Kataloge; `UiMessage` hält Textvorlagen und Daten getrennt. Hardwaretypen behalten ihre technischen Filterwerte. Windows-Traytexte folgen dem Sprachwechsel; native Traybedienung ist damit nicht geprüft.

`RuntimeHardeningTests` und der native Desktop-Smoke ergänzen zehn Views, Hilfe/About, fehlende Medienanbieter, Diagnose und Shutdown. CI startet echte Windows- und Linux-X11-Prozesse. `tools/packaging/smoke_portable.py` prüft zusätzlich frische Archivextraktion, Prüfsumme, native Architektur und Paketstart für Windows x64 sowie Linux x64/ARM64. ARM64 läuft auf `ubuntu-24.04-arm`. `build_debian.py` und `smoke_debian.sh` ergänzen amd64-/arm64-Installation auf Debian 12/13, Desktopdatei, GUI, isolierten Keyring und Deinstallation. Native Tray-Klicks, OS-Anmeldung/-Abmeldung, Dateiauswahl und Livequellen bleiben manuelle Zielsystemprüfungen; alle automatisierten Smokes verwenden ein virtuelles Gerät.

## Referenzen

App und Protokolltester verwenden Avalonia. Die WPF-Referenz ist über Git-Commit `922ca1b` zugänglich. Das [Paritätsaudit](wpf-avalonia-parity-audit.md) enthält das quellengeprüfte Inventar der 126 benannten WPF-Tab-Controls und 71 tabbezogenen XAML-Handlerverwendungen sowie den aktuellen Avalonia-Abgleich.

Protokollunsicherheiten werden ausschließlich in [protocol.md](../protocol.md) und [reverse-engineering.md](../reverse-engineering.md) gepflegt. Nächste Implementierungsschritte stehen in [TODO](../../TODO.md).
