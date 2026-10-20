# Avalonia-Portierung: aktueller Arbeitsstand

Stand: 20.10.2026. Branch: `feature/linux-avalonia`.
Implementierungsstand: PORT-01.

Diese Datei beschreibt den aktuellen Stand. Anforderungen stehen im [PRP](linux-avalonia-port.md), priorisierte Restarbeiten in [TODO](../../TODO.md), Plattform- und Projektgrenzen im [Inventar](linux-port-inventory.md).

## Aktuelle Verifikation

Lokal auf Windows mit .NET SDK 10.0.112 und Zielruntime .NET 8 geprüft:

- `dotnet.exe test tests/LIS2.App.Avalonia.Tests/LIS2.App.Avalonia.Tests.csproj -c Release --no-restore`: Exit 0, 304/304 Tests erfolgreich, keine übersprungenen Tests. Ergebnisdatei `TestResults/avalonia-port01.trx` im Testprojekt.
- Sieben Fälle in `PageWorkspaceActionsTests`: gültiger/abgewiesener Editorentwurf, persistierte neue Seite, virtuelle/serielle/beide/keine laufende Sequenz, leere Workspace-Wiederherstellung und transaktionale Ablehnung ungültiger gespeicherter Seiten.
- Gegenprobe ohne Entwurfsschutz und mit ignorierter leerer Liste: sechs fehlgeschlagene Tests. Isolierte Gegenprobe ohne Sequenz-Reload: drei fehlgeschlagene Tests für die aktiven Transportkombinationen. Regulären Code wiederhergestellt und gesamte Testsuite erfolgreich ausgeführt.

Der CI-Lauf für dieses Paket steht noch aus. Die vorhandene Branch-CI prüft Windows/Ubuntu mit .NET 8 und drei Pakete. Die Avalonia-Tests prüfen Headless-Klassen einschließlich des gemeinsamen Aktionsablaufs mit echten Sequenzcontrollern und temporären Workspace-Dateien. Die konkreten GUI-Controls und Quellenhandler werden nicht durch Klicktests geprüft.

Der Workflow startet keine Avalonia-Oberfläche. Die Paketprüfung kontrolliert Dateien und Größen; sie startet weder das extrahierte Programm noch ARM64 auf einem ARM64-System. WPF-Smoke-/Screenshottests aus dem Windows-Workflow sind kein Avalonia-Nachweis.

## Implementierung nach Codebefund

| Bereich | Code und vorhandene Abdeckung | Grenze des Nachweises |
| --- | --- | --- |
| Runtime | `DisplayRuntimeHost`, `PageSequenceController`, `PagePlayback`, `PageSourceValues`, adaptive/periodische Schleifen; Tests für Ticks, Stop/Drain, Reload und fehlerhafte Ausgabe | Teilweise UI-Orchestrierung; Plattformverträge noch nicht durchgängig implementiert |
| Display | Virtuelle und serielle Session, 20x2-Frames, Helligkeit, ROM-/Glyphenkommandos, unveränderte Frames werden unterdrückt | Keine physische Prüfung; Avalonia-Livepfad verwendet nicht den differenziellen `DisplayFrameWriter` |
| Pages | Pflege, Reihenfolge, Aktivierung, Timing, Sichtbarkeit, Presets, JSON-Validierung, animierte Entwurfsvorschau, Variablenbrowser; Quellenaktionen mit gemeinsamem Entwurfsschutz und Reload; leere Workspace-Liste | GUI-Interaktionen nicht automatisiert |
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

Diese Befunde stammen aus dem Quellvergleich. Sie wurden nicht durch eine GUI reproduziert.

- `PortableWorkspace` enthält weder Transportmodus/Port noch einen eigenen HA-Aktivierungszustand.
- `_preferredBrightness` wird geladen und gespeichert; Display-/Serial-Helligkeitsaktionen aktualisieren dieses Feld nicht, und beim Verbindungsaufbau wird es nicht angewendet.
- Die allgemeine Displaybedienung verwendet `_virtualDevice`; serielle Aktionen liegen in separaten, teilweise ausgeblendeten Controls. Einheitliche Bedienung des gewählten Transports ist damit nicht nachgewiesen.
- `IPlatform*`-Verträge sind definiert; die konkreten Adapter implementieren sie nicht. `LIS2.Runtime` kompiliert mehrere Dateien aus dem Avalonia-Verzeichnis per Link, ohne Avalonia-Paketabhängigkeit.

## Verifikation und Produktentscheidungen

- **Automatisiert geprüft:** 304/304 lokale Avalonia-Tests und die genannten Gegenproben.
- **Quellseitig geprüft:** Projektziele, Workflow-Inhalt, Ansichten/Handler, Session- und Persistenzpfade sowie zugehörige Testdateien.
- **Nicht in diesem Auftrag ausgeführt:** Avalonia-GUI-Tests, Desktop-Sichtprüfung, Live-HA-/Player-/Sensorprüfung und physische LIS2-Prüfung.
- **Verbindlich:** eine gemeinsame Avalonia-Oberfläche; WPF bleibt Referenz bis zur Paritätsabnahme. Modaler Variablenbrowser ist genehmigt. Physische Geräteprüfung wird getrennt ausgewiesen.
- **Offene Widersprüche:** WMP Legacy aktiv trotz zurückgestelltem Umfang; automatischer lokaler Tokenstore-Pfad; zusätzliche oder versteckte Bedienelemente gegenüber WPF.

## Weiterarbeit

Die priorisierte Liste und das nächste zusammenhängende Arbeitspaket stehen in [TODO](../../TODO.md). Kein alter fehlgeschlagener oder erfolgreicher Zwischencommit ersetzt den aktuellen CI-Nachweis. Bei neuem Code-HEAD diese Datei und den zugehörigen Workflow-Lauf erneut abgleichen.
