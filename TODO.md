# TODO: Avalonia-Portierung

Stand: 20.10.2026. Branch: `feature/linux-avalonia`.
Implementierungsstand: PORT-02. Lokal 326/326 Avalonia-Tests auf Windows erfolgreich; aktueller CI-Nachweis im [Status](docs/prp/linux-port-status.md).

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

## PORT-01: Quellenaktionen und Seitenzustand

Seitenerzeugung aus HA, Medien und Hardware-Presets verwendet `PageWorkspaceActions` für Entwurfsschutz, Auswahl, Liveübernahme und Speicherung.

- [x] HA-, Medien- und Hardware-Seitenerzeugung aktualisiert laufende virtuelle und serielle Sequenzen.
- [x] Quellenaktionen validieren und übernehmen Editorentwürfe vor einem Listen-/Editorwechsel; ungültige Entwürfe verhindern die Aktion.
- [x] Gespeicherte leere Seitenliste beim Laden übernehmen; ohne Workspace-Datei die Startseite erhalten.
- [x] Sieben Regressionstests für gültige/ungültige Entwürfe, beide Transports, gespeicherte leere Liste und ungültige gespeicherte Seiten. Gegenproben erkennen ausgelassenen Entwurfsschutz, fehlendes Reload und ignorierte leere Liste.
- [x] [CI 39440198251](https://github.com/Eitschman/lis2-control-center/actions/runs/39440198251) für Code-Commit `f372ed0`: alle sieben Jobs erfolgreich; je OS 304/304 Avalonia-Tests sowie Core/Display/Fans erfolgreich, drei Pakete erstellt. Nachweise im Status/Paritätsaudit aktualisiert.

Betroffen: `PageWorkspaceActions`, Quellenhandler, Workspace-Laden und Avalonia-Tests. Die Nachweise prüfen den gemeinsamen Ablauf headless, keine echten GUI-Klicks, HA-Verbindungen oder serielle Hardware.

Nächstes Implementierungspaket: **PORT-03: Diagnose und Laufzeitlebenszyklus**.

## Weitere Implementierungs- und Prüfaufgaben

### PORT-02: Einstellungen und Gerätebedienung

- [x] Transportmodus, gewählten Port und HA-Aktivierung speichern und wiederherstellen. Fehlender gespeicherter Port bleibt ausgewählt; explizites HA-Aus bleibt auch bei vorhandenem Token erhalten.
- [x] Helligkeitsänderungen speichern und beim Verbinden anwenden, ebenso konfigurierte Glyphen.
- [x] Allgemeine Zeilen-, Clear-, Demo-, ROM-, Glyphen-, Helligkeits- und Seitenaktionen an den angewendeten Transport anbinden. Ein noch nicht angewendeter ComboBox-Wechsel ändert das Ziel nicht.
- [x] Windows-Import sichtbar in Settings; Transport, Port, HA-Aktivierung und Hardwarepräferenzen zusätzlich übernehmen. Token bleibt getrennt; bestehende HA-Verbindung wird beendet und bei Bedarf ausdrücklich neu verbunden.
- [x] Dashboard-Handausgabe mit WPF-Kanalgrenzen und AllowStop, ohne Änderung des gespeicherten Regelmodus. Serielle Freigabe je Aktion, erster Fan-Befehl 100 Prozent und Sicherheitswiederherstellung bei Stop/Trennen. Keine Handausgabe parallel zur automatischen Regelung.
- [x] Regressionen für Persistenz, Import, Transportzuordnung, manuelle Lüfter und Fehlerpfade; tatsächliche Controls, Tastatur-Trennen und gerenderte Dashboard-/Display-/Settings-Ansichten bei Normal- und Mindestgröße. Desktop-/Hardwareprüfungen bleiben getrennt offen.

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
- [ ] Bestehenden Display-Formatierungstest kulturfest formulieren: deutscher Windows-Lauf ergibt korrektes Komma, Test erwartet fest einen Dezimalpunkt; invarianter Lauf besteht mit 48/48. Produktformatierung nach `CurrentCulture` erhalten.
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
