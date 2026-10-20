# Avalonia-Portierung: aktueller Arbeitsstand

Stand: 20.10.2026. Branch: `feature/linux-avalonia`.
Implementierungsstand: PORT-03.
Geprüfter Code-Commit: `d2d2defdf4d92d0f868daca47ef8ab4bdc402e5e`.

Anforderungen stehen im [PRP](linux-avalonia-port.md), Restarbeiten in [TODO](../../TODO.md), Plattformgrenzen im [Inventar](linux-port-inventory.md) und Bedienungsunterschiede im [Paritätsaudit](wpf-avalonia-parity-audit.md).

## Aktuelle Verifikation

Lokal auf Windows mit .NET SDK 10.0.112 und Zielruntime .NET 8 geprüft:

- Avalonia: 345/345 Tests erfolgreich, Exit 0, keine übersprungenen Tests. Frischer Lauf `TestResults/help-all.trx`; Hilfedialog zusätzlich in `help-sidebar.trx` mit 6/6 Fällen geprüft.
- Hilfe: fünf Sprachen, Systemstandard und Sprachwechsel im offenen Dialog mit echten Controls geprüft. Katalogtests decken alle zehn Themen, leere und gefüllte Variablenlisten, unveränderte technische Schlüssel sowie die Ausführung von HA-Formatierungs-/Sichtbarkeitsbeispielen und numerischen Glyphenreferenzen ab. Die Gegenprobe vor der Korrektur scheiterte bei deutscher Sprache am englischen Dialogtitel (`help-language-negative.trx`, Exit 1).
- Sieben Fälle in `RuntimeHardeningTests` prüfen verzögerten erfolgreichen/fehlgeschlagenen Start gegen Shutdown, Fortsetzung nach Cleanup-Fehlern, differenzielle serielle/virtuelle Frames, Teilfehler, manuelle Cacheinvalidierung, Reconnect mit/ohne Lüfterfreigabe, begrenztes Journal sowie tatsächliche Ansichten und Dialoge.
- Gegenprobe: ausgelassener Start-Drain, fehlendes Cache-Reset nach Teilfehler und still ignorierte Provider-DLL führen zu vier gezielt fehlgeschlagenen Tests, Exit 1 (`port03-negative.trx`). Regulären Code wiederhergestellt und gesamte Suite mit Exit 0 geprüft.
- Wiederverbindung mit Lüfterfreigabe: fehlendes Cache-Reset reproduziert, 1/1 gezielter Test fehlgeschlagen (`port03-reconnect-negative.trx`); beide Connect-Pfade invalidieren den Cache.
- Nativer Windows-Desktopprozess: Exit 0; zehn Views, Hilfe/About, sichtbarer Fehler einer fehlenden GSMTC-DLL, Diagnosebericht und Shutdown-Drain. Ergebnisse unter `TestResults/native-smoke-checked`. Isolierte Stores und virtuelles Gerät; keine produktiven Quellen gestartet.
- Display: 48/48, Exit 0 (`port03-display.trx`, `-m:1 --environment DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`). Der vorhandene Test `Render_FormatsNumberPercentBytesAndAffixes` erwartet einen Dezimalpunkt; die Produktimplementierung folgt `CurrentCulture`. Dieser Kulturkonflikt bleibt in PORT-06 offen. Der lokale MSBuild-Mehrknotenlauf brach mit MSB4166 ab; der Einzelknotenlauf besteht.

Diagnose, Hilfe/About und Medienfehler wurden unter Windows und Linux gerendert und angesehen. Die Linux-Ansichten stammen aus dem nativen X11-Smoke; die korrigierte Fehlermeldung benennt das fehlende playerctl mit Installationshinweis. Die Fehleranzeige verwendet Textumbruch und rote Schrift. Screenshots liegen unter `TestResults/screenshots/port03-*.png` und in den nativen Smoke-Artefakten. Die Geräte-UI-Tests aus PORT-02 prüfen zusätzlich Dashboard/Display/Settings bei 1305 × 893 und 950 × 600 Pixeln sowie Trennen per Tastatur. Ein automatisierter Bildvergleich besteht nicht.

[CI 39455194970](https://github.com/Eitschman/lis2-control-center/actions/runs/39455194970) prüft den angegebenen Code-Commit mit .NET 8. Alle sieben Jobs sind erfolgreich: je OS 345/345 Avalonia-, 55/55 Core-, 48/48 Display- und 12/12 Fans-Tests sowie die drei Pakete `win-x64`, `linux-x64` und `linux-arm64`. Windows-Desktop-Smoke und Linux-X11-Smoke über Xvfb bestehen einschließlich Prozessende mit Exit 0. Beide verwenden Zeitlimits und isolierte Stores ohne physisches LIS2.

Artefakte: `Avalonia-device-views-Windows`/`Linux`, `Avalonia-desktop-smoke-Windows`/`Linux` sowie drei `LIS2-Avalonia-*`-Pakete. Die Desktop-Smokes enthalten je 13 gerenderte Ansichten, Diagnose vor/nach Shutdown und `result.txt`. WPF wird durch diesen Workflow nicht geprüft. Nachfolgende reine Dokumentationscommits ändern den hier geprüften Code nicht; der verlinkte Lauf gehört ausdrücklich zum angegebenen Commit.

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
| Hilfe/About | Kontextauswahl und zehn Themen in fünf Sprachen; gespeicherte Sprache, Systemstandard und offener Dialog geprüft; Inhalte gegen Code korrigiert; About mit Versions-/Laufzeitdaten und tatsächlichem Workspace-Ordner | Weitere WPF-Inhaltstiefe, About-Übersetzungen und Hilfeplatzierung offen; Runtime unterstützt nur numerische Glyphenschlüssel |
| Diagnose | UTC-Journal für Anwendung und tatsächliche virtuelle/serielle Transportergebnisse, OK/ERROR, dekodierte Befehle, aktueller Export | Maximal 1000 Einträge pro Journal und zehn serielle Sitzungen; kein persistentes Logarchiv |

## Verbindliche Grenzen und Weiterarbeit

Eine gemeinsame Avalonia-Oberfläche bleibt das Ziel; WPF bleibt Referenz bis zur Paritätsabnahme. Der modale Variablenbrowser ist genehmigt. Zusätzliche oder versteckte Bedienelemente gegenüber WPF, aktive WMP-Legacy-Integration und automatischer Tokenstore-Ausweichpfad bleiben ausdrücklich offene Befunde.

Native Dateiauswahl, Tray-Menübedienung, OS-Abmeldung, DPI-/Theme-/Sprachmatrix, Live-HA-/Player-/Sensorintegration, extrahierter Paketstart, ARM64-Ausführung auf ARM64 und physische LIS2-Ausgabe sind durch den Smoke nicht nachgewiesen.

Erster Schritt bei der nächsten Fortsetzung, vor PORT-04: **Übersetzungen auf allen zehn Menüseiten prüfen und fehlende Übersetzungen korrigieren**. Benutzerbefund aus der GUI: Auf allen Seiten stehen noch viele Texte auf Englisch. Der Nachweis für die lokalisierte Hilfe gilt nicht für die übrige Oberfläche. Umfang und Prüfschritte stehen in [TODO](../../TODO.md).

Danach folgt **PORT-04: Medien und Produktgrenzen**. Plattformverträge und ihre konkreten Abweichungen sind im Inventar abgeglichen; neue Linux-Desktopdienste gehören zu PORT-06.
