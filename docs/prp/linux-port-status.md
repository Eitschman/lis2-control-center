# Avalonia-Portierung: aktueller Arbeitsstand

Stand: 20.10.2026. Branch: `feature/linux-avalonia`.
Implementierungsstand: PORT-03. Aktueller GitHub-Nachweis wird nach dem Push ergänzt.

Anforderungen stehen im [PRP](linux-avalonia-port.md), Restarbeiten in [TODO](../../TODO.md), Plattformgrenzen im [Inventar](linux-port-inventory.md) und Bedienungsunterschiede im [Paritätsaudit](wpf-avalonia-parity-audit.md).

## Aktuelle Verifikation

Lokal auf Windows mit .NET SDK 10.0.112 und Zielruntime .NET 8 geprüft:

- Avalonia: 332/332 Tests erfolgreich, Exit 0, keine übersprungenen Tests. Frischer Lauf `TestResults/port03-all.trx`.
- Sechs Fälle in `RuntimeHardeningTests` prüfen verzögerten erfolgreichen/fehlgeschlagenen Start gegen Shutdown, Fortsetzung nach Cleanup-Fehlern, differenzielle serielle/virtuelle Frames, Teilfehler, manuelle Cacheinvalidierung, Reconnect, begrenztes Journal sowie tatsächliche Ansichten und Dialoge.
- Gegenprobe: ausgelassener Start-Drain, fehlendes Cache-Reset nach Teilfehler und still ignorierte Provider-DLL führen zu vier gezielt fehlgeschlagenen Tests, Exit 1 (`port03-negative.trx`). Regulären Code wiederhergestellt und gesamte Suite mit Exit 0 geprüft.
- Nativer Windows-Desktopprozess: Exit 0; zehn Views, Hilfe/About, sichtbarer Fehler einer fehlenden GSMTC-DLL, Diagnosebericht und Shutdown-Drain. Ergebnisse unter `TestResults/native-smoke-checked`. Isolierte Stores und virtuelles Gerät; keine produktiven Quellen gestartet.
- Display: 48/48, Exit 0 (`port03-display.trx`, `-m:1 --environment DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`). Der vorhandene Test `Render_FormatsNumberPercentBytesAndAffixes` erwartet einen Dezimalpunkt; die Produktimplementierung folgt `CurrentCulture`. Dieser Kulturkonflikt bleibt in PORT-06 offen. Der lokale MSBuild-Mehrknotenlauf brach mit MSB4166 ab; der Einzelknotenlauf besteht.

Diagnose, Hilfe/About und Medienfehler wurden gerendert und angesehen. Die Fehleranzeige verwendet Textumbruch und eine rote Markierung. Screenshots liegen unter `TestResults/screenshots/port03-*.png` und im nativen Smoke-Verzeichnis. Die Geräte-UI-Tests aus PORT-02 prüfen zusätzlich Dashboard/Display/Settings bei 1305 × 893 und 950 × 600 Pixeln sowie Trennen per Tastatur. Ein automatisierter Bildvergleich besteht nicht.

Der Workflow enthält einen nativen Windows-Smoke und Linux-X11-Smoke über Xvfb, jeweils mit Zeitlimit und Artefakten. Aktuelle CI-Ergebnisse sind bis zum abgeschlossenen Lauf offen. WPF wird durch diesen Workflow nicht geprüft.

## Implementierung nach Codebefund

| Bereich | Implementiert und geprüft | Grenze |
| --- | --- | --- |
| Runtime | Quellen-/Sequenzbesitzer, adaptive Schleifen, Start-Drain vor Dispose; Cleanup weiterer Ressourcen trotz Einzelfehlern | UI-Orchestrierung weiterhin teilweise in MainWindow; Vertragsabgleich im Inventar |
| Display | Gemeinsamer `DisplayFrameWriter` in virtueller und serieller Session; geänderte Spannen, Cache-Reset bei manuellen Befehlen, Reconnect und Teilfehlern | Physische Übernahme nicht geprüft |
| Pages | Pflege, Vorlagen, Timing, Sichtbarkeit, Presets, JSON-Validierung, animierte Vorschau, Variablenbrowser und Entwurfsschutz/Reload | Semantische Bedienparität weiter offen |
| Events/Glyphen | Ereignisqueue, Ablauf/Abbruch, acht Glyphenslots, Dateien, Namen und Spektrumset | Dialogparität und physische Zeichen offen |
| Hardware/Fans | Windows LibreHardwareMonitor, Linux procfs/sysfs/hwmon; vier Kanäle, Grenzen, Hysterese, Fail-safe, virtuelle/serielle Regelung und Handausgabe | Reale Treiber, Sensorwechsel und Sicherheitszustände nicht geprüft |
| Medien | Windows Winamp/GSMTC und aktive WMP-Legacy-Quelle; Linux playerctl; sichtbare fehlende Provider, abgebrochene MPRIS-Prozesse werden beendet | WMP-Legacy-Aktivierung widerspricht Produktumfang; Position/Playerwahl und Liveplayer offen |
| Home Assistant | WebSocket, Entitäten/Attribute, Aliase/Favoriten, Clipboard und Seitenerzeugung | Keine eigene aktuelle Liveprüfung oder Linux-Abnahme |
| Einstellungen | Persistierter Transport/Port/HA-Aktivierung, Helligkeit, Windows-Import, Theme/Sprache, Windows-Tray/Autostart | Native Dateiauswahl und Linux-Desktopintegration offen |
| Tokenstore | DPAPI, secret-tool, lokaler AES-Dateipfad und Migration | Automatischer AES-Ausweichpfad in PORT-05 zu klären |
| Hilfe/About | Kontextauswahl, zehn Themen, Versions-/Laufzeitdaten und tatsächlicher Workspace-Ordner; Dialog-Smoke | Übersetzungen und WPF-Inhaltstiefe offen |
| Diagnose | UTC-Journal für Anwendung und tatsächliche virtuelle/serielle Transportergebnisse, OK/ERROR, dekodierte Befehle, aktueller Export | Maximal 1000 Einträge pro Journal und zehn serielle Sitzungen; kein persistentes Logarchiv |

## Verbindliche Grenzen und Weiterarbeit

Eine gemeinsame Avalonia-Oberfläche bleibt das Ziel; WPF bleibt Referenz bis zur Paritätsabnahme. Der modale Variablenbrowser ist genehmigt. Zusätzliche oder versteckte Bedienelemente gegenüber WPF, aktive WMP-Legacy-Integration und automatischer Tokenstore-Ausweichpfad bleiben ausdrücklich offene Befunde.

Native Dateiauswahl, Tray-Menübedienung, OS-Abmeldung, DPI-/Theme-/Sprachmatrix, Live-HA-/Player-/Sensorintegration, extrahierter Paketstart, ARM64-Ausführung auf ARM64 und physische LIS2-Ausgabe sind durch den Smoke nicht nachgewiesen.

Nächstes Paket: **PORT-04: Medien und Produktgrenzen**. Plattformverträge und ihre konkreten Abweichungen sind im Inventar abgeglichen; neue Linux-Desktopdienste gehören zu PORT-06.
