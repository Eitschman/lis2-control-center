# Architektur

Stand: 20.10.2026. Geltungsbereich: `feature/linux-avalonia`, Arbeitspaket PORT-02.
Implementierungs- und Prüfnachweise stehen im [Status](prp/linux-port-status.md).

## Projekte und Abhängigkeiten

`LIS2.App.Avalonia` ist die gemeinsame Desktopanwendung für Windows und Linux. Auch `tools/LIS2.ProtocolTester` verwendet Avalonia. Der Quellbaum und die Solution enthalten keine WPF-Projekte. Die WPF-Funktionsreferenz ist über Git-Commit `922ca1b` zugänglich.

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

Die portablen Projekte verwenden `net10.0`. `Directory.Build.props` gibt weiterhin `net10.0-windows10.0.19041.0` vor; portable Projekte überschreiben diesen Wert. GSMTC und mehrere Windows-Test-/Simulatorprojekte erben das Windows-Ziel. Der Protokolltester verwendet ausdrücklich `net10.0`.

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

`PageFrameDispatcher` unterdrückt identische ganze Frames und löscht bei leerer Ausgabe. `RenderBothAsync` verwendet in `VirtualDeviceSession` und `UnifiedLis2Session` den differenziellen `DisplayFrameWriter` aus `LIS2.Display`. Direkte Zeilenausgaben, Clear und ROM-Ausgaben setzen dessen Zustand zurück.

Ereignisse können eine temporäre Sequenz starten. Nach Ablauf wird zur normalen Seite zurückgekehrt oder die nur für das Ereignis gestartete Ausgabe beendet. Manuelle Bildausgaben stoppen und leeren ausstehende Sequenzschreibvorgänge, bevor sie selbst schreiben. Helligkeit und Glyphenprogrammierung drainieren nur den Timer und verwenden das Frame-Gate von `PageSequenceController.RunExclusiveAsync`. Zeitplan, Scrollzustand und Ereignisse bleiben erhalten; nur ein zuvor laufender Timer wird wieder aufgenommen. Fehler stoppen die betroffene Sequenz.

## Gerätezugriff und Lüfter

`IDisplaySession` bündelt Displaybefehle beider Sessions. `MainWindow.DeviceActions` wählt das Ziel anhand des angewendeten, gespeicherten Transportmodus. Änderungen der Transportauswahl werden erst beim Anwenden wirksam. Der Wechsel speichert zuerst, drainiert beide Sequenzen, trennt alte Sessions und verbindet genau den gewählten Transport. Helligkeit und konfigurierte Glyphen werden beim Verbinden angewendet. Die serielle Vorschau zeigt erfolgreich gesendete Inhalte, keine Hardware-Rückmeldung.

Die Dashboard-Handausgabe verwendet `PortableFanSettings.CalculateManualOutputs` und `FanController` für Minimum, Maximum und AllowStop. Der gespeicherte Regelmodus bleibt erhalten. `SerialDisplaySession.ApplyManualFansAsync` benötigt ausdrückliche Freigabe, teilt das Lifecycle-Gate mit Start/Stop/Disconnect und lehnt parallele automatische Regelung ab. Erstfreigabe schreibt 100 Prozent; Stop, Trennen und Schreibfehler entziehen die Freigabe und versuchen die Sicherheitsausgabe. Die UI verbraucht jede manuelle Freigabe nach einer Aktion.

`UnifiedLis2Session` schützt serielle Display-, Glyphen-, Helligkeits- und Lüfterkommandos mit einem gemeinsamen Semaphore-Gate. Die normale Verbindung startet ohne Lüfterfreigabe. `SerialDisplaySession.StartFansAsync` setzt nach ausdrücklichem UI-Consent zuerst vier 100-%-Werte, aktiviert die `FanRuntime` und startet die zweisekündige `FanRuntimeLoop`.

Nach erfolgreichem Speichern übergibt `UpdateRunningFanConfigurationAsync` die vier Kanäle an die laufende serielle/virtuelle Regelung und den kontinuierlichen Dry-run. `FanRuntime.ConfigureAsync` übernimmt die Konfiguration unter ihrem Gate und verwirft den alten Hysterese-/Ausgabeverlauf. Der nächste Regeltakt verwendet diese Werte. Konfiguration aktiviert weder eine gestoppte Sitzung noch eine neue Lüfterfreigabe.

Die Freigabe wird nicht gespeichert. Stop/Fehler/Disconnect sollen die Regelung leeren, die Freigabe widerrufen und den Sicherheitsframe anfordern. Ein gescheiterter Hardware-Schreibvorgang ist keine Garantie für den tatsächlichen Lüfterzustand. Virtuelle Fehlereinspeisung prüft Reihenfolge und Softwareverhalten.

Zusätzlich existieren `AuthorizedFanRuntimeSession`, `SafeSerialFanSession`, Dry-run und eine separate virtuelle Lüftertestsession. Die UI ist kein Beleg dafür, dass alle Helfer denselben produktiven Pfad verwenden. Quellenverlust und ungültige Sensorwerte werden in der Runtime gegen Fail-safe ausgewertet.

Es gibt keinen automatischen seriellen Wiederverbindungsablauf. Displayverbindungen können bereits vor der späteren Lüfterfreigabe genutzt werden. Die Architektur behauptet deshalb keine sichere Lüfterinitialisierung vor jeder Displayausgabe beim normalen Connect.

## Datenquellen und Plattformen

- `PageSourceValues` verwendet die gemeinsame Registry, Clock und HA sowie ergänzte Hardware-/Mediensnapshots. Die vorhandenen Template-Namensräume bleiben erhalten.
- Windows-Hardwaresnapshots stammen aus `WindowsHardwareFanSensors` mit LibreHardwareMonitor; Linux nutzt `LinuxSystemSensors` und `LinuxHwmonFanSensors`.
- Windows startet Winamp, WMP Legacy mit getrennten Metadaten-/Audiopipes und den per Reflection geladenen GSMTC-Anbieter. Die Legacy-Medienkarte und `WmpLegacy.*` berücksichtigen die unabhängigen Verbindungsfristen; Spektrumglyphen verwenden dessen feste Byte-Pegelskala.
- Linux liest sekündlich die Playerliste und ein XML-maskiertes Metadatenformat über playerctl. Media.* enthält Player, State, Title, Artist, Album, Elapsed, Duration und Connected. Die gespeicherte Playerauswahl bleibt bei Abwesenheit getrennt; veraltete Antworten werden verworfen. Abbruch und Timeout beenden den Kindprozess. Es besteht keine eigene MPRIS-D-Bus-Subscription.
- Die `IPlatform*`-Schnittstellen sind definiert. Konkrete Adapter werden momentan direkt benutzt und implementieren diese Verträge nicht.

## Persistenz und Geheimnisse

`PortableWorkspaceStore` schreibt unter `Environment.SpecialFolder.LocalApplicationData/LIS2ControlCenter/avalonia-workspace.json`. Schreibvorgänge werden innerhalb der Store-Instanz serialisiert und über eine temporäre Datei ersetzt. Fehlerhaftes JSON wird umbenannt. Ein prozessübergreifendes Lock ist damit nicht implementiert.

Der Workspace enthält Seiten, Glyphen, Lüftereinstellungen, Transportmodus, Port, HA-Aktivierung/-URL/-Präferenzen und die lokale Linux-Tokenwahl, Hardwarepräferenzen, Variablenfavoriten, Helligkeit, Theme, Sprache und Glyph-on-connect. Explizites HA-Aus wird unabhängig vom Token wiederhergestellt; bei fehlendem Aktivierungsfeld bleibt das tokenbasierte Verhalten erhalten. Vorhandene Workspace-Dateien ersetzen die Seiten auch mit einer leeren Liste. Ohne Datei bleibt die Startseite erhalten. Die Importvalidierung erfolgt vor Änderungen am geladenen Zustand.

`WindowsSettingsImporter` validiert bekannte Felder einschließlich Geräteauswahl, HA-Aktivierung und Hardwarepräferenzen, ohne Windows-Tokens zu importieren. Sein Button ist unter Einstellungen sichtbar. Der UI-Ablauf speichert vor dem Anwenden, drainiert und trennt bisherige Geräte und beendet die HA-Verbindung. Anschließend verbindet er den importierten Gerätetransport; HA wird bei Bedarf ausdrücklich neu verbunden.

`PortableHaTokenStore` verwendet Windows DPAPI bzw. Linux `secret-tool`. Unter Linux wählt der Benutzer lokale AES-GCM-Dateien ausdrücklich über `HomeAssistantLocalTokenStorage`; fehlender oder gesperrter Secret Service wechselt die Speicherart nicht. Schlüssel und Chiffretext liegen getrennt vom Workspace nebeneinander mit Unix-Dateirechten 0600. Dies schützt nicht gegen Zugriff mit demselben Benutzerkonto. Im nativen Modus werden lokale Dateien erst nach erfolgreichem Speichern und Rücklesen entfernt. Löschfehler bleiben sichtbar; HA wird trotzdem gestoppt und seine Deaktivierung gespeichert. Der Schlüsselbundprozess liest stdout/stderr parallel und hat eine Frist von zehn Sekunden. `PageSourceValues` ergänzt originale HA-Schlüssel um normalisierte Aliase einschließlich Einheiten und Attributen; Stop und Verbindungsabbruch entfernen alte Quellwerte.

## Desktop und Lebenszyklus

`App.axaml.cs` bindet das Avalonia-Tray auf Windows und Linux an. Öffnen und Beenden folgen dem Sprachwechsel; Beenden wartet auf `BeginShutdownAsync`. Windows minimiert/schließt in den Infobereich. Linux hält minimierte Fenster in der Taskleiste und beendet beim Schließen, damit der Prozess auch ohne Tray-Host erreichbar bleibt. Die Anmeldung verwendet unter Windows den Benutzer-Run-Schlüssel und unter Linux `DesktopAutostart` mit dem eigenen XDG-Eintrag `autostart/lis2-control-center.desktop`. Pfade werden ohne Shell maskiert; beim Start über `dotnet` wird auch der Assemblypfad eingetragen.

`DesktopInstance` hält einen exklusiven Dateihandle unter dem Benutzerprofil und nimmt Aktivierungsanforderungen über eine auf denselben Benutzer beschränkte Named Pipe entgegen. Folgestarts öffnen kein zweites Gerät. Entscheidend ist der offene Handle, nicht die nach Ende verbleibende Sperrdatei. `StartupSplash` wird vor der Hauptfensterinitialisierung gerendert. Wiederanzeigen initialisiert Quellen und Transport nicht erneut. Native Desktop-Smokes prüfen die Aktivierung durch einen separaten Prozess.

`PageSourceValues` bietet numerische und gespeicherte benannte Glyphenvariablen an. Die Namensnormalisierung entspricht WPF; doppelte Namen bezeichnen den letzten Slot, numerische Namen überschreiben keine Slotnummer. `HelpVariableCatalog` beschreibt statische Schlüssel auch ohne verbundene Quelle und verwendet die eigenen Hilfeübersetzungen unter `LIS2.App.Avalonia/Help`. Datums-/Wochentagswerte behalten beim Start der gemeinsamen Clock-Quelle ihren Vertrag.

`MainWindow` initialisiert Quellen/Timer einmalig und drainiert sie beim Schließen über `BeginShutdownAsync`. UI-Medienrefresh, Hardwareanzeige, Ereignisanzeige und Editorpreview enthalten weiterhin Dispatcher-Timer. Native Desktop-Smokes prüfen das Prozessende zusätzlich zu den Stop-/Drain-Verträgen.

Theme-Ressourcen und fünf Sprachkataloge existieren. Die Hilfe wird aus `HelpReferenceCatalog` mit zehn Kontextthemen aufgebaut; About zeigt Version, Runtime, Architektur, OS und Datenverzeichnis. Vollständige Übersetzungs-/Darstellungsparität ist nicht nachgewiesen.

## Diagnose und Prüfung

Virtueller Zustand, dekodierte letzte 200 Kommandos und Textexport sind vorhanden. Die Avalonia-Diagnose erfasst noch kein echtes Anwendungs-/Protokolllog.

GitHub Actions prüft Core, Display, Fans und Avalonia-Headless-Tests auf Windows und Ubuntu und publiziert drei Pakete. GUI-Navigation, extrahierter Paketstart, ARM64-Ausführung und physisches LIS2 sind davon nicht abgedeckt. Konkrete Restarbeiten stehen in [TODO](../TODO.md).
