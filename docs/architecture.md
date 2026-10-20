# Architektur

Stand: 20.10.2026. Geltungsbereich: `feature/linux-avalonia`, Arbeitspaket PORT-01.
Implementierungs- und Prüfnachweise stehen im [Status](prp/linux-port-status.md).

## Projekte und Abhängigkeiten

`LIS2.App.Avalonia` ist die gemeinsame Desktopanwendung für Windows und Linux. `LIS2.App` enthält die WPF-Referenz und bleibt bis zur Paritätsabnahme erhalten. WPF wird nicht an die neue Runtime angebunden.

| Projekt | Tatsächliche Zuständigkeit |
| --- | --- |
| `LIS2.Core` | Protokoll, Zeichenkodierung, `Lis2Device`, serielle/virtuelle Transporte, Glyphen und virtueller Interpreter |
| `LIS2.Display` | Vorlagen, Sichtbarkeit, Scheduler, Scrolling, Ereignisse und `DisplayFrameWriter` |
| `LIS2.Fans` | Berechnung von Fixed/Curve/Follow/External/Off, Grenzen und Hysterese |
| `LIS2.Sources` | `IDataSource`, Registry, Clock, HA-WebSocket und LibreHardwareMonitor-Quellcode |
| `LIS2.Runtime` | Quellenlebenszyklus, Lüfterruntime/-schleife, Linux-Sensoren, Plattformverträge und verlinkte Display-Runtime |
| `LIS2.Winamp` / `LIS2.WmpLegacy` | Named-Pipe-Quellen, Snapshots und Variablen |
| `LIS2.WindowsMedia` | Windows-GSMTC-Anbieter, separat gebaut und für Windows-Pakete mitgeliefert |
| `LIS2.App.Avalonia` | AXAML, partielle MainWindow-Dateien, Plattformadapter, Workspace, Tokenstore, Hilfe/About und UI-Integration |

Die portablen Projekte verwenden `net8.0`. `Directory.Build.props` gibt weiterhin `net8.0-windows10.0.19041.0` vor; portable Projekte überschreiben diesen Wert. WPF, GSMTC und mehrere Windows-Test-/Toolprojekte erben das Windows-Ziel.

Mehrere UI-unabhängige Dateien liegen physisch unter `src/LIS2.App.Avalonia` und werden in `LIS2.Runtime.csproj` per `Compile Include/Link` eingebunden. Die App schließt deren eigene Kompilierung mit `Compile Remove` aus. Diese Klassen behalten teilweise den Namespace `LIS2.App.Avalonia`, die Runtime referenziert jedoch kein Avalonia-Paket. Eine andere physische Ablage oder ein durchgängiges ViewModel-/DI-Modell ist kein bereits umgesetzter Zustand.

## Displaypfad

```text
Clock / Hardware / HA / Winamp / WindowsMedia / Media
                       |
                 PageSourceValues
                       |
                DisplayRuntimeHost
                 /             \
 PageSequenceController     gemeinsame EventQueue
          |
     PagePlayback
          |
 PageFrameDispatcher
      /       \
 VirtualDeviceSession   SerialDisplaySession
                              |
                       UnifiedLis2Session
                              |
                          Lis2Device
                              |
                     ILis2Transport
```

`DisplayRuntimeHost` besitzt Quellen, Ereignisqueue und erzeugte Sequenzen. Jede Sequenz serialisiert Ticks, Reload und ausstehende Schreibvorgänge. Virtuelle und serielle Wiedergabe haben getrennte Sequenzen und Ausgabeläufe, verwenden aber denselben Seitenworkspace und dieselben priorisierten Ereignisse.

`PagePlayback` verarbeitet Sichtbarkeit, Rotation, Vorlagen-Refresh und beide Scrollzeilen. `NextDelay` steuert `AdaptiveRuntimeLoop`; die UI-Anbindung erfolgt über `UiRuntimeDispatch`. Der Editor verwendet eine unabhängige `PageEditorPreview` und einen eigenen UI-Timer.

`PageWorkspaceActions` verbindet die Seitenerzeugung aus HA, Medien und Hardware-Presets mit dem Editor und der Wiedergabe. Ungültige Editorentwürfe verhindern Änderungen an Liste, Auswahl und Persistenz. Gültige Entwürfe werden übernommen, danach folgen neue Seite, Auswahl, Reload beider laufender Sequenzen, Speicherung und Navigation zum Editor. Die UI liefert diese Aktionen als Delegates; der Ablauf wird mit echten Sequenzcontrollern und temporären Workspace-Dateien headless geprüft.

`PageFrameDispatcher` unterdrückt identische ganze Frames und löscht bei leerer Ausgabe. Der Avalonia-Livepfad ruft `RenderBothAsync` auf und nutzt den differenziellen `DisplayFrameWriter` aus `LIS2.Display` nicht. Die in der WPF-/Displaybibliothek vorhandene Ausgabe geänderter Spannen ist deshalb kein Nachweis für diesen Pfad.

Ereignisse können eine temporäre Sequenz starten. Nach Ablauf wird zur normalen Seite zurückgekehrt oder die nur für das Ereignis gestartete Ausgabe beendet. Manuelle Aktionen stoppen und leeren ausstehende Sequenzschreibvorgänge, bevor sie selbst schreiben.

## Gerätezugriff und Lüfter

`UnifiedLis2Session` schützt serielle Display-, Glyphen-, Helligkeits- und Lüfterkommandos mit einem gemeinsamen Semaphore-Gate. Die normale Verbindung startet ohne Lüfterfreigabe. `SerialDisplaySession.StartFansAsync` setzt nach ausdrücklichem UI-Consent zuerst vier 100-%-Werte, aktiviert die `FanRuntime` und startet die zweisekündige `FanRuntimeLoop`.

Die Freigabe wird nicht gespeichert. Stop/Fehler/Disconnect sollen die Regelung leeren, die Freigabe widerrufen und den Sicherheitsframe anfordern. Ein gescheiterter Hardware-Schreibvorgang ist keine Garantie für den tatsächlichen Lüfterzustand. Virtuelle Fehlereinspeisung prüft Reihenfolge und Softwareverhalten.

Zusätzlich existieren `AuthorizedFanRuntimeSession`, `SafeSerialFanSession`, Dry-run und eine separate virtuelle Lüftertestsession. Die UI ist kein Beleg dafür, dass alle Helfer denselben produktiven Pfad verwenden. Quellenverlust und ungültige Sensorwerte werden in der Runtime gegen Fail-safe ausgewertet.

Es gibt keinen automatischen seriellen Wiederverbindungsablauf. Displayverbindungen können bereits vor der späteren Lüfterfreigabe genutzt werden. Die Architektur behauptet deshalb keine sichere Lüfterinitialisierung vor jeder Displayausgabe beim normalen Connect.

## Datenquellen und Plattformen

- `PageSourceValues` verwendet die gemeinsame Registry, Clock und HA sowie ergänzte Hardware-/Mediensnapshots. Die vorhandenen Template-Namensräume bleiben erhalten.
- Windows-Hardwaresnapshots stammen aus `WindowsHardwareFanSensors` mit LibreHardwareMonitor; Linux nutzt `LinuxSystemSensors` und `LinuxHwmonFanSensors`.
- Windows startet Winamp, den per Reflection geladenen GSMTC-Anbieter und derzeit auch WMP Legacy. Letzteres widerspricht der zurückgestellten Produktentscheidung und ist als offener Punkt geführt.
- Linux fragt mit `playerctl metadata --format` periodisch Status/Titel/Interpret/Album/Dauer ab. Das ist keine eigene MPRIS-D-Bus-Subscription; Zeitposition und Playerwahl fehlen.
- Die `IPlatform*`-Schnittstellen sind definiert. Konkrete Adapter werden momentan direkt benutzt und implementieren diese Verträge nicht.

## Persistenz und Geheimnisse

`PortableWorkspaceStore` schreibt unter `Environment.SpecialFolder.LocalApplicationData/LIS2ControlCenter/avalonia-workspace.json`. Schreibvorgänge werden innerhalb der Store-Instanz serialisiert und über eine temporäre Datei ersetzt. Fehlerhaftes JSON wird umbenannt. Ein prozessübergreifendes Lock ist damit nicht implementiert.

Der Workspace enthält Seiten, Glyphen, Lüftereinstellungen, HA-URL/-Präferenzen, Hardwarepräferenzen, Variablenfavoriten, Helligkeit, Theme, Sprache und Glyph-on-connect. Transportmodus, Port und eigener HA-Aktivierungszustand fehlen. Die gespeicherte Helligkeit wird im Livepfad noch nicht angewendet. Vorhandene Workspace-Dateien ersetzen die Seiten auch mit einer leeren Liste. Ohne Datei bleibt die Startseite erhalten. Die Importvalidierung erfolgt vor Änderungen am geladenen Zustand.

`WindowsSettingsImporter` validiert und übernimmt bekannte Felder, ohne Windows-Tokens zu importieren. Sein Button liegt in einem unsichtbaren erweiterten Settings-Container.

`PortableHaTokenStore` verwendet Windows DPAPI bzw. Linux `secret-tool`. Bei fehlendem Secret Service schreibt der Linux-Pfad automatisch AES-GCM-Chiffretext und einen lokalen Schlüssel. Migration in den nativen Store wird versucht. Dieser vorhandene Ausweichpfad ist ein offener Freigabe-/Sicherheitsbefund, keine zugesicherte Gleichwertigkeit zum nativen Store.

## Desktop und Lebenszyklus

Windows-Tray, Minimieren/Schließen ins Tray und Run-Autostart mit `--minimized` sind in `App.axaml.cs` und `MainWindow.Settings.cs` implementiert. Linux erhält diese Tray-/Autostartanbindung derzeit nicht. Single Instance und Splash sind im Avalonia-Pfad nicht umgesetzt.

`MainWindow` initialisiert Quellen/Timer und räumt sie im asynchronen `Closed`-Handler auf. UI-Medienrefresh, Hardwareanzeige, Ereignisanzeige und Editorpreview enthalten weiterhin Dispatcher-Timer. Die Existenz von Stop/Drain-Methoden ersetzt keinen GUI-Prozessende-Test.

Theme-Ressourcen und fünf Sprachkataloge existieren. Die Hilfe wird aus `HelpReferenceCatalog` mit zehn Kontextthemen aufgebaut; About zeigt Version, Runtime, Architektur, OS und Datenverzeichnis. Vollständige Übersetzungs-/Darstellungsparität ist nicht nachgewiesen.

## Diagnose und Prüfung

Virtueller Zustand, dekodierte letzte 200 Kommandos und Textexport sind vorhanden. Die Avalonia-Diagnose erfasst noch kein echtes Anwendungs-/Protokolllog.

GitHub Actions prüft Core, Display, Fans und Avalonia-Headless-Tests auf Windows und Ubuntu und publiziert drei Pakete. GUI-Navigation, extrahierter Paketstart, ARM64-Ausführung und physisches LIS2 sind davon nicht abgedeckt. Konkrete Restarbeiten stehen in [TODO](../TODO.md).
