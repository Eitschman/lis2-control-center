# TODO: Avalonia-Portierung

Stand: 20.10.2026. Branch: `feature/linux-avalonia`.
Geprüfte Codebasis: `71b72a8987a4f9b8a14a3807f94667569e28be43`.
[CI 39435226116](https://github.com/Eitschman/lis2-control-center/actions/runs/39435226116): sieben von sieben Jobs erfolgreich.

Diese Liste gilt für den Avalonia-Branch. [PRP](docs/prp/linux-avalonia-port.md), [Status](docs/prp/linux-port-status.md) und [Paritätsaudit](docs/prp/wpf-avalonia-parity-audit.md) unterscheiden vorhandenen Code, automatische Tests und offene Desktop-/Hardwareprüfung.

## Vorhandene Basis

- [x] Gemeinsame Avalonia-App für Windows/Linux und portable .NET-8-Bibliotheken.
- [x] Headless Seiten-/Ereignisruntime, adaptive Schleifen und virtuelle/serielle Sessionintegration.
- [x] Seiteneditor mit Presets, Sichtbarkeit, Scrolling, Entwurfsvorschau und validiertem JSON-Import/-Export.
- [x] Genehmigter modaler Variablenbrowser mit Suche, Livewerten und persistenten Favoriten.
- [x] Glypheneditor, Dateien, acht Slots und Spektrum-Ersetzungsdialog.
- [x] Windows-/Linux-Sensoren, Aliase, Favoriten und fehlende Sensoren im Katalog.
- [x] Lüfterruntime mit Grenzen, Fail-safe, Hysterese, Simulation und ausdrücklich freizugebender serieller Ausgabe.
- [x] Winamp, Windows GSMTC, HA-WebSocket-Quelle und Linux-Metadaten über `playerctl`.
- [x] Workspace, Windows-Importlogik, Theme-/Sprachauswahl, Windows-Tray und Autostart.
- [x] Hilfe mit zehn Themen, About-Dialog, virtuelle Diagnose und Textexport.
- [x] Windows-/Ubuntu-Builds und vier Testprojekte je OS; Pakete für `win-x64`, `linux-x64`, `linux-arm64` mit SHA-256.

Abgehakt bedeutet vorhandener und im Status eingegrenzter Umfang, keine pauschale WPF- oder Hardwareparität.

## Nächstes Arbeitspaket: Quellenaktionen und Seitenzustand

**PORT-01.** Seitenerzeugung aus HA/Media und Workspace-Wiederherstellung müssen denselben Entwurfsschutz und dieselbe Liveübernahme wie die normalen Seitenaktionen verwenden.

- [ ] `CreateHaPageAsync` muss laufende virtuelle und serielle Sequenzen nach Seitenerzeugung aktualisieren.
- [ ] HA-/Media-Seitenerzeugung muss bestehende Editorentwürfe vor einem Listen-/Editorwechsel validieren und erhalten.
- [ ] Eine gespeicherte leere Seitenliste muss beim Laden als leere Liste übernommen werden.
- [ ] Regressionen für gültige/ungültige Entwürfe, beide Transports und leere Workspace-Wiederherstellung ergänzen.
- [ ] Aktuellen Windows-/Ubuntu-CI-Lauf prüfen und Nachweise in Status/Paritätsaudit aktualisieren.

Betroffen: `MainWindow.HomeAssistant.cs`, `MainWindow.Media.cs`, `MainWindow.Pages.cs`, `MainWindow.Workspace.cs` und die vorhandenen Avalonia-Tests. Keine Erweiterung des Medienumfangs und keine Änderung der Lüfterfreigabe.

## Weitere Implementierungs- und Prüfaufgaben

### PORT-02: Einstellungen und Gerätebedienung

- [ ] Transportmodus, gewählten Port und HA-Aktivierung gemäß WPF-Verhalten speichern und wiederherstellen.
- [ ] Helligkeitsänderungen mit gespeicherter Einstellung und Verbindungsaufbau verbinden.
- [ ] Allgemeine Display-/Glyphen-/Helligkeitsaktionen an den ausgewählten Transport anbinden; separate versteckte Serial-Controls ersetzen keine WPF-Bedienparität.
- [ ] Windows-Importer: übernommene Felder und fehlende Hardwarepräferenzen abgleichen. Importlogik existiert, der UI-Zugang liegt in einem unsichtbaren Container.
- [ ] WPF-Parität der Dashboard-Handausgabe klären/umsetzen; Slider und Apply sind derzeit deaktiviert. Lüfterfreigabe bleibt verbindlich.

### PORT-03: Diagnose und Laufzeitlebenszyklus

- [ ] Echtes Anwendungs-/Protokoll-Logging einschließlich Export implementieren.
- [ ] Avalonia-GUI-Smoke für Start, zehn Ansichten, Hilfe/About, fehlende Medienanbieter und geordnetes Beenden auf Windows/Linux ergänzen.
- [ ] Start-/Stop-/Drain-Pfade aller Quellen und Timer einschließlich verzögertem Medienstart gegen Shutdown prüfen.
- [ ] Differenzielle Display-Schreibspannen im Avalonia-Livepfad nachweisen; derzeit werden nur identische ganze Frames unterdrückt.
- [ ] Vorhandene `IPlatform*`-Verträge, Adapter und UI-Orchestrierungen gegen die Zielarchitektur abgleichen.

### PORT-04: Medien und Produktgrenzen

- [ ] Linux-MPRIS-Position, Playerauswahl, Lifecycle und Fehleranzeige ergänzen. Aktuell periodische `playerctl`-Metadatenabfrage, keine eigene D-Bus-Subscription.
- [ ] VLC und Plexamp einzeln auf realen Zielsystemen prüfen.
- [ ] GSMTC-Paket auf Windows mit/ohne Player und fehlender Provider-DLL starten.
- [ ] WMP-Legacy-Aktivierung gegen den zurückgestellten Produktumfang korrigieren oder ausdrücklich entscheiden lassen. Der Avalonia-Code startet die Quelle und zeigt ihre Karte unter Windows.
- [ ] Keine Linux-VU-/Spektrumanalyse aus MPRIS-Metadaten ableiten. Wine/Winamp bleibt optional.

### PORT-05: Home Assistant und Tokenstore

- [ ] HA-Verbindung, Reconnect, Attribute/Aliase, Deaktivierung und Persistenz unter Linux sowie Windows gezielt prüfen.
- [ ] Automatischen AES-Dateipfad bei fehlendem Secret Service gegen die Freigabepolitik klären; aktuelle Implementierung nicht als genehmigte Produktentscheidung behandeln.
- [ ] `secret-tool`-/DBus-/Keyring-Voraussetzungen und erkennbare Fehlerzustände auf den Zielsystemen prüfen.
- [ ] Migration/Entfernung alter Tokendateien und Zugriffsrechte mit gültigen und fehlerhaften Fällen absichern.

### PORT-06: WPF-Abgleich und Desktopintegration

- [ ] Die zehn Ansichten anhand der [aktuellen Matrix](docs/prp/wpf-avalonia-parity-audit.md) semantisch prüfen, einschließlich Fehler-, Leer-, Auswahl- und Speicherpfaden.
- [ ] Hilfeinhalt/-tiefe und Übersetzungen gegen die WPF-Referenz prüfen; neue Avalonia-Hilfetexte sind überwiegend Englisch.
- [ ] Hilfeplatzierung, Tray-Menütexte und weitere hart codierte Laufzeittexte abgleichen.
- [ ] Themes, Tastatur/Fokus, DPI, Fenstergrenzen und lange Texte in allen fünf Sprachen prüfen.
- [ ] Zusätzliche Bedienflächen und nicht ausdrücklich geforderte Titelzusätze im bearbeiteten UI-Bereich bereinigen; genehmigten Variablenbrowser erhalten.
- [ ] Linux-Tray, Autostart, Single Instance und Splash gegenüber dem PRP-Ziel bearbeiten.
- [ ] WPF erst nach Funktions-/Darstellungsabgleich und Benutzerabnahme entfernen.

### PORT-07: Pakete und Zielsysteme

- [ ] Extrahierte Windows-/Linux-Pakete starten; GUI-Smoke ergänzt die bestehende Dateiprüfung.
- [ ] Linux ARM64 auf echter ARM64-Laufzeit prüfen. Cross-Publish allein zählt nicht als Laufzeitnachweis.
- [ ] Debian/KDE und Raspberry Pi 5 mit Distribution, Architektur, DPI, Displayserver und Abhängigkeiten protokollieren.
- [ ] GTK-/X11-/Wayland-/DBus-/Keyring-Abhängigkeiten und serielle Zugriffsrechte anhand tatsächlicher Zielsysteme dokumentieren.
- [ ] Support- und Lizenzstatus der eingesetzten .NET-/Avalonia-Versionen vor Release anhand offizieller Quellen prüfen; die Versionsangaben im Projekt sind dafür kein Nachweis.
- [ ] Avalonia-Releaseumschaltung und Versionsmetadaten erst nach den PRP-Abnahmekriterien vornehmen; Tag-Releases veröffentlichen derzeit WPF.

## Physische Validierung, getrennt von Softwarepaketen

Diese Prüfungen sind offen. Mangels LIS2-Gerät sind sie kein Blocker für rein softwareseitige Arbeitspakete.

- [ ] Unterstützte Befehle mit 19200 Baud/8N1, Display, Helligkeit und acht Glyphenslots prüfen.
- [ ] Trailing-`00`, Display-off und sichere Geräteidentifikation ausschließlich anhand bestätigter Befunde entscheiden.
- [ ] Sichere Lüfterausgänge, Stopverhalten, Sensorverlust sowie Disconnect/Reconnect kontrolliert prüfen.
- [ ] Sensoridentität nach Geräte-/Treiberwechsel und tatsächliche Windows-/Linux-Zugriffsrechte prüfen.
- [ ] Bestätigte Protokollbefunde in [protocol.md](docs/protocol.md) festhalten.
- [ ] Historische Kanalbelastbarkeit nur mit Quelle und Verifikationsstatus dokumentieren.

## Geltende Produktgrenzen

Keine unbekannten automatischen Protokollkommandos. Kein direkter Gerätezugriff aus Player-Plug-ins. HA bleibt lesend. Keine eigenen OctoPrint-/Plex-/Docker-Quellen für bereits über HA verfügbare Werte. Kein First-run-Wizard und kein Restore-defaults-Workflow. Der vorhandene Windows-Migrationsimport ist von einem allgemeinen Einstellungsimport/-export zu unterscheiden.

Optionale spätere Ideen wie Seitenübergänge, Profile, generische HTTP-Datenquellen, rohe serielle Mitschnitte oder schönere JSON-Attributdarstellung sind keine Voraussetzungen der Portierung und werden nur bei konkretem Bedarf beauftragt.
