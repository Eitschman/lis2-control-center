# Avalonia-Portierung: aktueller Arbeitsstand

Stand: 20.10.2026. Branch: `feature/linux-avalonia`.
Codebasis: `71b72a8987a4f9b8a14a3807f94667569e28be43`.

Diese Datei beschreibt den aktuellen Stand. Anforderungen stehen im [PRP](linux-avalonia-port.md), priorisierte Restarbeiten in [TODO](../../TODO.md), Plattform- und Projektgrenzen im [Inventar](linux-port-inventory.md).

## Nachgewiesene CI

[GitHub Actions 39435226116](https://github.com/Eitschman/lis2-control-center/actions/runs/39435226116) prüft die genannte Codebasis. Alle sieben Jobs sind erfolgreich:

| Job | Ergebnis |
| --- | --- |
| Avalonia shell (ubuntu-latest) | Build und Avalonia-Testprojekt erfolgreich |
| Avalonia shell (windows-latest) | Build und Avalonia-Testprojekt erfolgreich |
| .NET 8 shared libraries (ubuntu-latest) | Core-, Display- und Fans-Tests erfolgreich |
| .NET 8 shared libraries (windows-latest) | Core-, Display- und Fans-Tests erfolgreich |
| Portable Avalonia app (linux-x64) | Veröffentlichung, Paketprüfung und Upload erfolgreich |
| Portable Avalonia app (linux-arm64) | Cross-Publish, Paketprüfung und Upload erfolgreich |
| Portable Avalonia app (win-x64) | Veröffentlichung einschließlich GSMTC-DLL, Paketprüfung und Upload erfolgreich |

Die Avalonia-Tests prüfen Headless-Klassen. Der Workflow startet keine Avalonia-Oberfläche. Die Paketprüfung kontrolliert Dateien und Größen; sie startet weder das extrahierte Programm noch ARM64 auf einem ARM64-System. WPF-Smoke-/Screenshottests aus dem Windows-Workflow sind kein Avalonia-Nachweis. Dokumentationsänderungen nach der genannten Codebasis wurden von diesem Lauf nicht geprüft.

Die Logs dieses Laufs weisen je Betriebssystem 55/55 Core-, 48/48 Display-, 12/12 Fans- und 297/297 Avalonia-Tests als erfolgreich aus, jeweils ohne übersprungene Tests. Das sind 412 erfolgreiche Testausführungen je OS, keine Coverage-Angabe.

## Implementierung nach Codebefund

| Bereich | Code und vorhandene Abdeckung | Grenze des Nachweises |
| --- | --- | --- |
| Runtime | `DisplayRuntimeHost`, `PageSequenceController`, `PagePlayback`, `PageSourceValues`, adaptive/periodische Schleifen; Tests für Ticks, Stop/Drain, Reload und fehlerhafte Ausgabe | Teilweise UI-Orchestrierung; Plattformverträge noch nicht durchgängig implementiert |
| Display | Virtuelle und serielle Session, 20x2-Frames, Helligkeit, ROM-/Glyphenkommandos, unveränderte Frames werden unterdrückt | Keine physische Prüfung; Avalonia-Livepfad verwendet nicht den differenziellen `DisplayFrameWriter` |
| Pages | Pflege, Reihenfolge, Aktivierung, Timing, Sichtbarkeit, Presets, JSON-Validierung, animierte Entwurfsvorschau, modaler Variablenbrowser | GUI-Interaktionen nicht automatisiert; Quellenaktionen schützen Entwürfe nicht durchgängig |
| Events | Gemeinsame priorisierte Ereignisqueue für virtuelle und serielle Sequenzen, Ablauf/Abbruch, temporäre Sequenz und Übergang zu normalen Seiten | Dialog-/Bedienparität offen |
| Glyphen | Acht Slots, Bitmap-Editor, benannte Schlüssel, Dateien, Spektrumset und Ersetzungsdialog; Workspace-/Protokolltests | Keine bestätigten physischen Zeichen |
| Hardware | Windows LibreHardwareMonitor; Linux procfs/sysfs/hwmon; Katalog, Filter, Aliase, Favoriten, fehlende Sensoren und Presets | Hardware-/Treiberwechsel und Zielsystemrechte offen |
| Fans | Vier Kanäle mit Fixed/Curve/Follow/External/Off, Hysterese, Grenzen, Fail-safe, Dry-run, virtuelle und explizit freizugebende serielle Regelung | Sichere Hardwarezustände werden nur simuliert geprüft; Dashboard-Handausgabe ist deaktiviert |
| Medien | Windows Winamp/GSMTC und aktive WMP-Legacy-Quelle; Linux `playerctl`; getrennte Template-Namensräume und Invalidierung alter Metadaten getestet | WMP-Legacy-Aktivierung widerspricht dem zurückgestellten Umfang; Linux-Position/Playerwahl fehlen |
| Home Assistant | Originale WebSocket-Quelle, Entitäten/Attribute, Aliase/Favoriten, Clipboard und Seitenerzeugung | Dokumentierte Benutzerbestätigung betrifft Windows-Konnektivität; keine eigene aktuelle Liveprüfung oder Linux-Abnahme |
| Einstellungen | `avalonia-workspace.json`, validierender Windows-Importer, Themen/Sprachen; Windows-Tray und Run-Autostart | Transport/Port/HA-Aktivierung fehlen im Workspace; Helligkeit wird gespeichert, aber nicht angewendet; Importzugang liegt in unsichtbarem Container |
| Tokenstore | Windows DPAPI; Linux Secret Service über `secret-tool`; lokaler AES-Dateipfad und Migration | Automatischer AES-Ausweichpfad ist eine offene Abweichung von der Freigabepolitik |
| Hilfe/About | `HelpReferenceCatalog`, Dialoge, Kontextauswahl aus zehn Hauptthemen, Live-Schlüssel, Glyphenreferenz und Versions-/Laufzeitdaten; Katalogtests | Dialoge nicht als GUI getestet; Hilfeinhalte überwiegend Englisch, Übersetzungs- und Inhaltstiefenabgleich offen |
| Diagnose | Snapshot, virtuelle Zustände, letzte 200 dekodierte Kommandos und Textexport | Protokolllog enthält weiterhin einen Hinweis auf fehlende Erfassung |

## Konkrete offene Integrationsbefunde

Diese Befunde stammen aus dem Quellvergleich. Sie wurden in diesem Dokumentationsauftrag nicht durch eine GUI reproduziert.

- `CreateHaPageAsync` erzeugt und speichert eine Seite, ruft jedoch `ReloadRunningPagesAsync` nicht auf.
- `CreateHaPageAsync` und `CreateMediaPageAsync` verwenden vor dem Neuladen des Editors nicht `CommitPendingPageEditor`.
- `ApplyPortableWorkspace` ersetzt Seiten nur bei `saved.Pages.Count > 0`; eine gespeicherte leere Liste wird nicht übernommen.
- `PortableWorkspace` enthält weder Transportmodus/Port noch einen eigenen HA-Aktivierungszustand.
- `_preferredBrightness` wird geladen und gespeichert; Display-/Serial-Helligkeitsaktionen aktualisieren dieses Feld nicht, und beim Verbindungsaufbau wird es nicht angewendet.
- Die allgemeine Displaybedienung verwendet `_virtualDevice`; serielle Aktionen liegen in separaten, teilweise ausgeblendeten Controls. Einheitliche Bedienung des gewählten Transports ist damit nicht nachgewiesen.
- `IPlatform*`-Verträge sind definiert; die konkreten Adapter implementieren sie nicht. `LIS2.Runtime` kompiliert mehrere Dateien aus dem Avalonia-Verzeichnis per Link, ohne Avalonia-Paketabhängigkeit.

## Verifikation und Produktentscheidungen

- **Automatisiert geprüft:** die oben genannten sieben CI-Jobs der Codebasis.
- **Quellseitig geprüft:** Projektziele, Workflow-Inhalt, Ansichten/Handler, Session- und Persistenzpfade sowie zugehörige Testdateien.
- **Nicht in diesem Auftrag ausgeführt:** lokale .NET-Tests, Avalonia-GUI-Tests, Desktop-Sichtprüfung, Live-HA-/Player-/Sensorprüfung und physische LIS2-Prüfung.
- **Verbindlich:** eine gemeinsame Avalonia-Oberfläche; WPF bleibt Referenz bis zur Paritätsabnahme. Modaler Variablenbrowser ist genehmigt. Physische Geräteprüfung wird getrennt ausgewiesen.
- **Offene Widersprüche:** WMP Legacy aktiv trotz zurückgestelltem Umfang; automatischer lokaler Tokenstore-Pfad; zusätzliche oder versteckte Bedienelemente gegenüber WPF.

## Weiterarbeit

Die priorisierte Liste und das nächste zusammenhängende Arbeitspaket stehen in [TODO](../../TODO.md). Kein alter fehlgeschlagener oder erfolgreicher Zwischencommit ersetzt den aktuellen CI-Nachweis. Bei neuem Code-HEAD diese Datei und den zugehörigen Workflow-Lauf erneut abgleichen.
