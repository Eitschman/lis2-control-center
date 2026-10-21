# Avalonia-Portierung: aktueller Arbeitsstand

Stand: 21.10.2026. Branch: `feature/linux-avalonia`.
Implementierungsstand: PORT-03 mit lokalisiertem Menüumfang vor PORT-04.
Geprüfter lokaler Arbeitsstand: Übersetzungspaket vor PORT-04. Der CI-Nachweis für diesen Stand wird über den zugehörigen Commit-Lauf ergänzt.

Anforderungen stehen im [PRP](linux-avalonia-port.md), Restarbeiten in [TODO](../../TODO.md), Plattformgrenzen im [Inventar](linux-port-inventory.md) und Bedienungsunterschiede im [Paritätsaudit](wpf-avalonia-parity-audit.md).

## Aktuelle Verifikation

Lokal auf Windows mit .NET SDK 10.0.112 und Zielruntime .NET 8 geprüft:

- Avalonia: 359/359 Tests erfolgreich, Exit 0, keine übersprungenen Tests (`TestResults/menu-release.trx`). Die 14 neuen Katalog-/Sprachfälle betreffen statische AXAML-Texte, fünf Sprachen und Systemstandard, zehn Ansichten, About, Sprachwechsel, Statusmeldungen, unveränderte Daten, Lüftermodi und Formatargumente.
- Gegenproben: Die deutsche Lüfterüberschrift blieb vor der Ergänzung englisch (`menu-language-negative.trx`). Musiktitel wie Connected wurden vor der Eingrenzung der statischen Übersetzung verändert; 6/6 Fälle erkennen das (`menu-data-negative.trx`, Exit 1). Ein falscher französischer Formatindex wird vom Katalogtest erkannt (`menu-placeholder-negative.trx`, 1/1 fehlgeschlagen). Regulären Stand wiederhergestellt und gesamte Suite mit Exit 0 geprüft.
- Hilfe: fünf Sprachen, Systemstandard und Sprachwechsel im offenen Dialog mit echten Controls geprüft. Katalogtests decken alle zehn Themen, leere und gefüllte Variablenlisten, unveränderte technische Schlüssel sowie die Ausführung von HA-Formatierungs-/Sichtbarkeitsbeispielen und numerischen Glyphenreferenzen ab. Die Gegenprobe vor der Korrektur scheiterte bei deutscher Sprache am englischen Dialogtitel (`help-language-negative.trx`, Exit 1).
- Sieben Fälle in `RuntimeHardeningTests` prüfen verzögerten erfolgreichen/fehlgeschlagenen Start gegen Shutdown, Fortsetzung nach Cleanup-Fehlern, differenzielle serielle/virtuelle Frames, Teilfehler, manuelle Cacheinvalidierung, Reconnect mit/ohne Lüfterfreigabe, begrenztes Journal sowie tatsächliche Ansichten und Dialoge.
- Gegenprobe: ausgelassener Start-Drain, fehlendes Cache-Reset nach Teilfehler und still ignorierte Provider-DLL führen zu vier gezielt fehlgeschlagenen Tests, Exit 1 (`port03-negative.trx`). Regulären Code wiederhergestellt und gesamte Suite mit Exit 0 geprüft.
- Wiederverbindung mit Lüfterfreigabe: fehlendes Cache-Reset reproduziert, 1/1 gezielter Test fehlgeschlagen (`port03-reconnect-negative.trx`); beide Connect-Pfade invalidieren den Cache.
- Nativer Windows-Desktopprozess: Exit 0; zehn Views, Hilfe/About, sichtbarer Fehler einer fehlenden GSMTC-DLL, Diagnosebericht und Shutdown-Drain. Ergebnisse unter `TestResults/native-smoke-checked`. Isolierte Stores und virtuelles Gerät; keine produktiven Quellen gestartet.
- Display: 48/48, Exit 0 (`port03-display.trx`, `-m:1 --environment DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`). Der vorhandene Test `Render_FormatsNumberPercentBytesAndAffixes` erwartet einen Dezimalpunkt; die Produktimplementierung folgt `CurrentCulture`. Dieser Kulturkonflikt bleibt in PORT-06 offen. Der lokale MSBuild-Mehrknotenlauf brach mit MSB4166 ab; der Einzelknotenlauf besteht.

Diagnose, Hilfe/About und Medienfehler wurden unter Windows und Linux gerendert und angesehen. Die Linux-Ansichten stammen aus dem nativen X11-Smoke; die korrigierte Fehlermeldung benennt das fehlende playerctl mit Installationshinweis. Die Fehleranzeige verwendet Textumbruch und rote Schrift. Screenshots liegen unter `TestResults/screenshots/port03-*.png` und in den nativen Smoke-Artefakten. Die Geräte-UI-Tests aus PORT-02 prüfen zusätzlich Dashboard/Display/Settings bei 1305 × 893 und 950 × 600 Pixeln sowie Trennen per Tastatur. Ein automatisierter Bildvergleich besteht nicht.

Der GitHub-Nachweis des Übersetzungspakets steht bis zum zugehörigen Branch-Lauf noch aus. Lokale Tests ersetzen keinen Windows-/Linux-CI-Nachweis oder Paketstart.

Artefakte: `Avalonia-device-views-Windows`/`Linux`, `Avalonia-desktop-smoke-Windows`/`Linux` sowie drei `LIS2-Avalonia-*`-Pakete. Die Desktop-Smokes enthalten je 13 gerenderte Ansichten, Diagnose vor/nach Shutdown und `result.txt`. WPF wird durch diesen Workflow nicht geprüft. Nachfolgende reine Dokumentationscommits ändern den hier geprüften Code nicht; der verlinkte Lauf gehört ausdrücklich zum angegebenen Commit.

Menü-Sichtprüfung unter Windows mit Skia: zehn Ansichten auf Deutsch bei 1305 × 893 und 950 × 600 einschließlich gescrollter Inhalte; zusätzliche französische und russische Menüseiten und About bei langen Übersetzungen. Screenshots: `TestResults/screenshots/menu-*.png`. Die Pixelmaße der schmalen Ansichten werden auf 950 × 600 geprüft. Aktionszeilen umbrechen; der Seiteneditor stapelt Liste und Eigenschaften bei schmalen Fenstern. Die Metadatenfelder und Listeneinträge sind von der statischen Übersetzung ausgenommen. Diagnosebeschriftungen sind lokalisiert; rohe Protokollbefehle, Journale und fremde Ausnahmeinformationen bleiben technische Originaldaten. Native Dateidialoge und Trayaktionen sind nicht über die Spracheinstellungen hinweg bedient worden.

## Implementierung nach Codebefund

Lokalisierte Hilfedialoge wurden unter Windows mit Skia bei 840 × 600 Pixeln gerendert und angesehen: Home Assistant in fünf Sprachen sowie Seitenhilfe auf Französisch und Lüfterhilfe auf Russisch. Dieselben deutschen, französischen und russischen Ansichten wurden aus dem Linux-CI-Artefakt angesehen. Überschriften und Absätze umbrechen; die Themenliste und Schließen bleiben sichtbar. Lange Inhalte werden vertikal gescrollt. Screenshots: `TestResults/screenshots/help-*.png` und `Avalonia-device-views-Linux`. Diese Prüfung verwendet isolierte Stores und ein virtuelles Gerät; sie ersetzt keine WPF-Inhaltstiefen- oder Hardwareabnahme.

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
| Hilfe/About | Kontextauswahl und zehn Themen in fünf Sprachen; gespeicherte Sprache, Systemstandard und offener Dialog geprüft; Inhalte gegen Code korrigiert; About mit Versions-/Laufzeitdaten und tatsächlichem Workspace-Ordner | Weitere WPF-Inhaltstiefe, Hilfeplatzierung offen; Runtime unterstützt nur numerische Glyphenschlüssel |
| Diagnose | UTC-Journal für Anwendung und tatsächliche virtuelle/serielle Transportergebnisse, OK/ERROR, dekodierte Befehle, aktueller Export | Maximal 1000 Einträge pro Journal und zehn serielle Sitzungen; kein persistentes Logarchiv |

## Verbindliche Grenzen und Weiterarbeit

Eine gemeinsame Avalonia-Oberfläche bleibt das Ziel; WPF bleibt Referenz bis zur Paritätsabnahme. Der modale Variablenbrowser ist genehmigt. Zusätzliche oder versteckte Bedienelemente gegenüber WPF, aktive WMP-Legacy-Integration und automatischer Tokenstore-Ausweichpfad bleiben ausdrücklich offene Befunde.

Native Dateiauswahl, Tray-Menübedienung, OS-Abmeldung, DPI-/Theme-/Sprachmatrix, Live-HA-/Player-/Sensorintegration, extrahierter Paketstart, ARM64-Ausführung auf ARM64 und physische LIS2-Ausgabe sind durch den Smoke nicht nachgewiesen.

Das vorgezogene Übersetzungspaket ist implementiert und lokal geprüft. Nächstes Paket: **PORT-04: Medien und Produktgrenzen**. Plattformverträge und ihre konkreten Abweichungen sind im Inventar abgeglichen; neue Linux-Desktopdienste gehören zu PORT-06.
