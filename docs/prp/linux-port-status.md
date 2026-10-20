# Avalonia-Portierung: aktueller Arbeitsstand

Stand: 20.10.2026. Branch: `feature/linux-avalonia`.
Implementierungsstand: PORT-02.
Geprüfter Code-Commit: `ac97be566664eefb70adf7a8a6f4275f7def9717`.

Diese Datei beschreibt den aktuellen Stand. Anforderungen stehen im [PRP](linux-avalonia-port.md), priorisierte Restarbeiten in [TODO](../../TODO.md), Plattform- und Projektgrenzen im [Inventar](linux-port-inventory.md).

## Aktuelle Verifikation

Lokal auf Windows mit .NET SDK 10.0.112 und Zielruntime .NET 8 geprüft:

- `dotnet.exe test tests/LIS2.App.Avalonia.Tests/LIS2.App.Avalonia.Tests.csproj -c Release --no-restore`: Exit 0, 326/326 Tests erfolgreich, keine übersprungenen Tests. Ergebnisdatei `TestResults/port02-all.trx` im Testprojekt.
- Zehn Fälle in `DeviceUiTests`: gespeicherter serieller Start, gemeinsame Display-/Glyphen-/ROM-/Helligkeits-/Seitenaktionen, Transportwechsel/Trennen, drei HA-Aktivierungsvarianten, sichtbarer Import, virtuelle/serielle Handausgabe und Rendering bei zwei Fenstergrößen. Tatsächliche Controls, isolierte Stores und simulierter serieller Transport; Hintergrundquellen sind abgeschaltet.
- Session-/Import-/Workspace-/Grenzentests prüfen explizite Freigabe, sicheren Erstframe, Stop/Trennen, Schreibfehler vor/nach Handausgabe, Ausschluss paralleler Regelung, gültige/ungültige Einstellungen und kompatible Defaults.
- Gegenproben: Vier gezielte Tests schlagen bei fehlender Helligkeitsanwendung, ignoriertem Import-Port, umgangenen Lüftergrenzen und fehlender Freigabeprüfung fehl. Ein weiterer Test schlägt bei falscher Transportzuordnung fehl. Regulären Code wiederhergestellt und gesamte Suite erfolgreich ausgeführt. Ergebnisse: `port02-negative.trx` und `port02-negative-routing.trx`.
- Core: 55/55, Fans: 12/12 erfolgreich. Display: unter deutscher Windows-Kultur 47/48; `Render_FormatsNumberPercentBytesAndAffixes` erwartet einen Dezimalpunkt, während die Implementierung gemäß `CurrentCulture` ein Komma ausgibt. Mit `--environment DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` bestehen 48/48. Ergebnisdateien: `port02-core.trx`, `port02-fans.trx`, `port02-display.trx` und `port02-display-invariant.trx` in den jeweiligen Testprojekten. Produktcode und bestehender Test wurden dafür nicht verändert.

[CI 39446107884](https://github.com/Eitschman/lis2-control-center/actions/runs/39446107884) prüft den genannten Code-Commit mit .NET 8. Alle sieben Jobs sind erfolgreich: Avalonia-Build/Tests und Core-/Display-/Fans-Tests jeweils auf Windows und Ubuntu sowie Veröffentlichung/Paketprüfung für `win-x64`, `linux-x64` und `linux-arm64`. Die aktuellen Joblogs weisen je OS 326/326 Avalonia-, 55/55 Core-, 48/48 Display- und 12/12 Fans-Tests aus, ohne Fehler oder übersprungene Tests. Beide Avalonia-Jobs haben je 14 gerenderte Ansichten hochgeladen. Nachfolgende reine Dokumentationscommits ändern den hier geprüften Code nicht; der genannte Lauf gehört zum angegebenen Commit.

Dashboard, Display und Settings wurden bei 1305 × 893 und 950 × 600 Pixeln mit Skia im Headless-Modus gerendert und angesehen, einschließlich unterer Scrollbereiche, gefüllter VFD-Vorschau und serieller Fan-Freigabe. Ein Tastaturtest trennt über die fokussierte Schaltfläche per Leertaste. Screenshots liegen unter `TestResults/screenshots/port02-*.png`; CI exportiert sie als `Avalonia-device-views-Windows` bzw. `Avalonia-device-views-Linux`. Es gibt keinen automatisierten Bildvergleich.

Die Headless-Tests erzeugen das tatsächliche Avalonia-Fenster, aber keinen nativen Desktop-/Tray-Prozess. Die Paketprüfung kontrolliert Dateien und Größen; sie startet weder das extrahierte Programm noch ARM64 auf einem ARM64-System. Native Dateiauswahl, echte HA-/Player-/Sensoranbieter und physische LIS2-Ausgabe sind damit nicht geprüft.

## Implementierung nach Codebefund

| Bereich | Code und vorhandene Abdeckung | Grenze des Nachweises |
| --- | --- | --- |
| Runtime | `DisplayRuntimeHost`, `PageSequenceController`, `PagePlayback`, `PageSourceValues`, adaptive/periodische Schleifen; Tests für Ticks, Stop/Drain, Reload und fehlerhafte Ausgabe | Teilweise UI-Orchestrierung; Plattformverträge noch nicht durchgängig implementiert |
| Display | Virtuelle und serielle Session, 20x2-Frames, Helligkeit, ROM-/Glyphenkommandos, unveränderte Frames werden unterdrückt | Keine physische Prüfung; Avalonia-Livepfad verwendet nicht den differenziellen `DisplayFrameWriter` |
| Pages | Pflege, Reihenfolge, Aktivierung, Timing, Sichtbarkeit, Presets, JSON-Validierung, animierte Entwurfsvorschau, Variablenbrowser; Quellenaktionen mit gemeinsamem Entwurfsschutz und Reload; leere Workspace-Liste | GUI-Interaktionen nicht automatisiert |
| Events | Gemeinsame priorisierte Ereignisqueue für virtuelle und serielle Sequenzen, Ablauf/Abbruch, temporäre Sequenz und Übergang zu normalen Seiten | Dialog-/Bedienparität offen |
| Glyphen | Acht Slots, Bitmap-Editor, benannte Schlüssel, Dateien, Spektrumset und Ersetzungsdialog; Workspace-/Protokolltests | Keine bestätigten physischen Zeichen |
| Hardware | Windows LibreHardwareMonitor; Linux procfs/sysfs/hwmon; Katalog, Filter, Aliase, Favoriten, fehlende Sensoren und Presets | Hardware-/Treiberwechsel und Zielsystemrechte offen |
| Fans | Vier Kanäle mit Fixed/Curve/Follow/External/Off, Hysterese, Grenzen, Fail-safe, Dry-run, virtuelle/serielle Regelung und Dashboard-Handausgabe; Freigabe und Sicherheitsframes | Sichere Hardwarezustände werden nur simuliert geprüft |
| Medien | Windows Winamp/GSMTC und aktive WMP-Legacy-Quelle; Linux `playerctl`; getrennte Template-Namensräume und Invalidierung alter Metadaten getestet | WMP-Legacy-Aktivierung widerspricht dem zurückgestellten Umfang; Linux-Position/Playerwahl fehlen |
| Home Assistant | Originale WebSocket-Quelle, Entitäten/Attribute, Aliase/Favoriten, Clipboard und Seitenerzeugung | Dokumentierte Benutzerbestätigung betrifft Windows-Konnektivität; keine eigene aktuelle Liveprüfung oder Linux-Abnahme |
| Einstellungen | Workspace mit Transport/Port/HA-Aktivierung; Helligkeitsanwendung bei Connect/Liveänderung; sichtbarer Windows-Import einschließlich Hardwarepräferenzen; Theme/Sprache und Windows-Tray/Run-Autostart | Nativer Dateidialog und Linux-Desktopintegration bleiben offen |
| Tokenstore | Windows DPAPI; Linux Secret Service über `secret-tool`; lokaler AES-Dateipfad und Migration | Automatischer AES-Ausweichpfad ist eine offene Abweichung von der Freigabepolitik |
| Hilfe/About | `HelpReferenceCatalog`, Dialoge, Kontextauswahl aus zehn Hauptthemen, Live-Schlüssel, Glyphenreferenz und Versions-/Laufzeitdaten; Katalogtests | Dialoge nicht als GUI getestet; Hilfeinhalte überwiegend Englisch, Übersetzungs- und Inhaltstiefenabgleich offen |
| Diagnose | Snapshot, virtuelle Zustände, letzte 200 dekodierte Kommandos und Textexport | Protokolllog enthält weiterhin einen Hinweis auf fehlende Erfassung |

## Konkrete offene Integrationsbefunde

Gerätebedienung und Einstellungen verwenden den angewendeten Transport; ein noch nicht bestätigter ComboBox-Wechsel verändert das Ausgabeziel nicht. Verbindungswechsel drainiert beide Sequenzen und verwirft Freigaben. Fehlende Ports werden nicht automatisch ersetzt. Der Import beendet die bestehende HA-Verbindung; bei Bedarf ausdrücklich neu verbinden.

- `IPlatform*`-Verträge sind definiert; die konkreten Adapter implementieren sie nicht. `LIS2.Runtime` kompiliert mehrere Dateien aus dem Avalonia-Verzeichnis per Link, ohne Avalonia-Paketabhängigkeit.

## Verifikation und Produktentscheidungen

- **Automatisiert geprüft:** 326/326 lokale Avalonia-Tests, die genannten Gegenproben und alle sieben CI-Jobs des angegebenen Code-Commits. Der separat ausgewiesene lokale Kulturfehler im bestehenden Display-Test bleibt offen.
- **Quellseitig geprüft:** Projektziele, Workflow-Inhalt, Ansichten/Handler, Session- und Persistenzpfade sowie zugehörige Testdateien.
- **Nicht in diesem Auftrag ausgeführt:** nativer Desktop-/Tray-Smoke, Live-HA-/Player-/Sensorprüfung, nativer Dateidialog und physische LIS2-Prüfung.
- **Verbindlich:** eine gemeinsame Avalonia-Oberfläche; WPF bleibt Referenz bis zur Paritätsabnahme. Modaler Variablenbrowser ist genehmigt. Physische Geräteprüfung wird getrennt ausgewiesen.
- **Offene Widersprüche:** WMP Legacy aktiv trotz zurückgestelltem Umfang; automatischer lokaler Tokenstore-Pfad; zusätzliche oder versteckte Bedienelemente gegenüber WPF.

## Weiterarbeit

Die priorisierte Liste und das nächste zusammenhängende Arbeitspaket stehen in [TODO](../../TODO.md). Kein alter fehlgeschlagener oder erfolgreicher Zwischencommit ersetzt den aktuellen CI-Nachweis. Bei neuem Code-HEAD diese Datei und den zugehörigen Workflow-Lauf erneut abgleichen.
