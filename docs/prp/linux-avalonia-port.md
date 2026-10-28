# PRP: LIS2 Control Center mit Avalonia

Stand: 22.10.2026. Hauptbranch: `main`.
Implementierungsstand: PORT-08 und Windows Media Player Legacy; genaue Prüfnachweise im [Status](linux-port-status.md).

## Ziel und verbindlicher Umfang

Eine gemeinsame Avalonia-Anwendung ersetzt die WPF-Anwendung auf Windows x64 und unterstützt Linux x64 sowie Linux ARM64. Die WPF-Funktionsreferenz für Funktionen, Anordnung, Interaktionen, Einstellungen und Vorlagen liegt im Git-Commit `922ca1b`. Der Benutzer beauftragt die Entfernung der WPF-Projekte und anschließend die Übernahme nach `main`. App und Protokolltester verwenden ausschließlich Avalonia; offene manuelle Desktop- und Hardwareprüfungen bleiben separat dokumentiert.

Die zehn Hauptansichten bleiben Dashboard, Display, Pages, Media, Events, Hardware, Fan Control, Settings, Diagnostics und Home Assistant. Windows unterstützt Winamp, GSMTC und Windows Media Player Legacy; Linux erhält die passenden Sensor- und Medienanbieter. Home Assistant bleibt lesend. Eigene OctoPrint-, Plex- oder Docker-Datenquellen sind für bereits über HA verfügbare Werte nicht vorgesehen.

Eine genehmigte UI-Abweichung ist der modale, durchsuchbare Variablenbrowser anstelle der großen WPF-ComboBox. Das unten beschriebene Dashboard-Mockup ist ebenfalls vom Benutzer freigegeben. Lüfterfreigabe und Sicherheitsgrenzen bleiben unabhängig von der Oberfläche verbindlich. Weitere Abweichungen werden im [Paritätsaudit](wpf-avalonia-parity-audit.md) erfasst, ohne sie dadurch zu genehmigen.

Das freigegebene Dashboard-Mockup ergänzt eine niedrige VFD-Karte über die gesamte Breite, Seitenwahl und Rotation-Pause bei unverändert kompakter Vorschau, erweiterte Lüfterdetails und vier konfigurierbare Livewerte. Schnellzugriff und Demo-Schaltfläche entfallen. Drehzahlangaben benötigen ausdrücklich zugeordnete Sensoren; die Oberfläche leitet keine Messwerte aus Stellprozenten ab. Livewerte verwenden den vorhandenen Variablenbrowser und Vorlagenrenderer. Die Auswahl wird im Workspace gespeichert. Hardwarefreigaben und Sicherheitsgrenzen bleiben erhalten.

## Arbeitsgrundlage und Nachweis

- [Implementierungsstatus](linux-port-status.md): aktueller Codebefund und CI-Nachweis.
- [Inventar](linux-port-inventory.md): Projekte, Zuständigkeiten, Plattformgrenzen und Tests.
- [Paritätsaudit](wpf-avalonia-parity-audit.md): vorhandene Ansichten und offene Unterschiede.
- [TODO](../../TODO.md): priorisierte Restarbeiten und nächstes Arbeitspaket.
- [Architektur](../architecture.md), [Protokoll](../protocol.md) und [Release-Verfahren](../release.md).

Builds, Tests, nativer Desktop-Smoke und Pakete werden über `.github/workflows/linux-portable.yml` geprüft. Lokal bestehen 451 Avalonia-Tests; der zusätzliche Linux-Schlüsselbundtest ist unter Windows übersprungen. HA-Verbindungsabläufe verwenden einen lokalen WebSocket-Protokollpartner. Linux-CI führt die Suite mit isoliertem echtem Secret Service aus; der aktuelle Nachweis steht im [Status](linux-port-status.md). Dieser ordnet CI-Läufe dem jeweiligen Code-Commit zu.

PORT-06B bindet gespeicherte Lüfterkanäle an laufende Regler und Dry-runs an, ohne Ausgaben zu aktivieren. Helligkeit und Glyphenprogrammierung erhalten die Seitenrotation und aktive Ereignisse. Seitenimporte melden Speicherfehler; die Bedingungsanzeige folgt Quelldaten und Sprache. Die zwölf zusätzlichen Regressionen verwenden isolierte Stores, tatsächliche Controls und virtuelle Protokollpartner für beide Transportpfade.

## Implementierungsstand

Abgehakte Punkte bezeichnen vorhandenen Code mit dem angegebenen Testumfang. Sie bedeuten weder Desktop-Abnahme noch physische Geräteprüfung.

| Bereich | Stand | Offener Umfang |
| --- | --- | --- |
| Projektstruktur | Avalonia 11.3.22, .NET 10 LTS, portable Bibliotheken und `LIS2.Runtime` vorhanden | Plattformverträge sind noch nicht durchgängig angebunden; UI-Code enthält weitere Orchestrierung |
| Display-Runtime | Scheduler, Scrolling, Quellen, Ereignisse und getrennte virtuelle/serielle Sequenzen; gemeinsame Geräteaktionen und differenzielle Spannen am angewendeten Transport | Spektrumvariable und Glyphenvorschau mit virtueller Ausgabe geprüft; physische Ausgabe offen |
| Seiteneditor | Seitenpflege, Vorlagen, Bedingungen, Presets, animierte Vorschau, Variablenbrowser, validierter JSON-Import/-Export und gemeinsamer Entwurfsschutz für Quellenaktionen vorhanden | Bedingungsfelder und Variablenbrowser angebunden; gemeinsame Hardware- und plattformgerechte Medienpresets geprüft |
| Glyphen | Acht 5x8-Slots, Dateien, Namen, Spektrumset und Ersetzungsdialog vorhanden | Physische Zeichenprüfung und UI-/Transportabgleich offen |
| Sensoren | LibreHardwareMonitor auf Windows; procfs/sysfs/hwmon auf Linux; Aliase, Favoriten und fehlende Sensoren berücksichtigt | Stabile Identitäten und Rechte auf Zielsystemen prüfen |
| Lüfter | Vier Modi plus Off, Kurven, Hysterese, Fail-safe, Simulation, freizugebende serielle Regelung und Dashboard-Handausgabe mit Kanalgrenzen | Weitere WPF-Interaktionen abgleichen; Hardware bleibt ungeprüft |
| Medien | Winamp/GSMTC auf Windows; Linux-playerctl mit gespeicherter Auswahl, Spielposition, Lifecycle und Fehleranzeige; WMP Legacy mit Metadaten, VU/Spektrum, Presets und zwei nativen Plug-ins | Reale VLC-/Plexamp-/Windows-Media-Player-Sitzungen offen |
| Home Assistant | WebSocket-Quelle, Reconnect und Stop, Entitäten, Attributwerte, wirksame Aliase, Favoriten und Seitenaktionen; ausdrückliche lokale Linux-Tokenauswahl | Reale HA-Instanz und Desktop-Schlüsselbund auf Zielsystemen prüfen |
| Einstellungen | Workspace einschließlich Transport/Port/HA-Aktivierung, Helligkeitsanwendung, sichtbarer Windows-Import, Theme/Sprache, Windows-/Linux-Tray und plattformeigener Autostart | Nativer Dateidialog, Tray-Bedienung und Anmeldung auf Zielsystemen offen |
| Hilfe und About | Kontextbezogene Hilfe mit zehn Themen in fünf Sprachen, Live-Schlüsseln, statischen Variablenbeschreibungen und numerischen/benannten Glyphen; F1 und Hilfe am Titel; About folgt der Sprache | Native Desktop-Bedienung und Benutzerabnahme offen |
| Diagnose | Zustandsübersicht, virtuelle Zustände, dekodierte Historie und Export mit zeitgestempeltem Anwendungs-/Transportjournal | Version, UTC, Transport/Port, Frame, Quellen-/Lüfterdetails und Start-/Absturzdatei vorhanden; nativen Dateidialog prüfen |
| CI und Pakete | Windows-/Ubuntu-Builds, vier Testprojekte je OS, nativer Desktop-Smoke und drei selbstständige Pakete mit Prüfsummen | Archivstart und echte ARM64-Ausführung fehlen |

## Prüffähige Checkliste

### Portable Laufzeit und Gerätezugriff

- [x] `LIS2.Core`, `LIS2.Display`, `LIS2.Fans`, `LIS2.Sources`, `LIS2.Winamp`, `LIS2.WmpLegacy` und `LIS2.Runtime` verwenden `net10.0`.
- [x] Headless Display-Runtime mit Quellen-Snapshots, Ereignisqueue, Scheduler, adaptiven Schleifen und Stop/Drain vorhanden.
- [x] Seitenänderungen laden laufende virtuelle und serielle Sequenzen neu; unveränderte Frames werden unterdrückt.
- [x] Headless Tests für Parallelität, fehlerhafte Schreibvorgänge, Ereignisprioritäten und Wiederaufnahme vorhanden.
- [x] Serielle Ports werden ohne Geräteöffnung ermittelt; Linux bevorzugt `/dev/serial/by-id`.
- [x] Serielle Display- und Lüfterkommandos verwenden den gemeinsamen Session-Gate.
- [x] Lüfter starten nur nach ausdrücklicher, nicht gespeicherter Freigabe. Der sichere 100-%-Frame wird vor der ersten Regelung gesendet.
- [x] UI-Orchestrierungen, Timer und sieben Plattformverträge gegen das Runtime-Ziel abgeglichen; konkrete Adapterabweichungen und Grenzen im Inventar.
- [x] Differenzielle Schreibspannen aus `DisplayFrameWriter` im virtuellen und seriellen Avalonia-Livepfad einschließlich Teilfehlern und Cacheinvalidierung nachgewiesen.
- [ ] Serielle Fehler, Wiederverbindung und sichere Ausgabe auf echten Zielsystemen getrennt prüfen.

Die normale serielle Verbindung startet als reine Displayverbindung. Sie schreibt ohne Lüfterfreigabe keine Lüfterwerte. Die Freigabe erfolgt auf der bereits geöffneten Verbindung; Displaykommandos können davor stattgefunden haben. Dies ist von der strengeren Zielvorstellung „sicherer Lüfterzustand vor jeder normalen Geräteausgabe“ zu unterscheiden.

### Datenquellen und Persistenz

- [x] Windows-/Linux-Sensoranbieter und getestete Sensorkataloge vorhanden.
- [x] Bestehende `Clock.*`, `Hardware.*`, `Winamp.*`, `WindowsMedia.*` und `HA.*`-Schlüssel werden weiterverwendet.
- [x] Linux-Medien liefern `Media.Connected`, `Media.State`, `Media.Title`, `Media.Artist`, `Media.Album`, `Media.Elapsed`, `Media.Duration` und `Media.Player` über `playerctl`.
- [x] Gespeicherte Linux-Playerauswahl, Position, Trennen/Wiederfinden und Abbruch/Timeout implementiert und getestet. Der Anbieter fragt sekündlich ab; eine eigene D-Bus-Subscription bleibt offen.
- [ ] VLC und Plexamp einzeln auf dem Zielsystem prüfen. Wine/Winamp bleibt optional.
- [x] Workspace und validierende Windows-Importlogik einschließlich Seiten, Glyphen und Lüfterkonfiguration vorhanden.
- [x] Windows-DPAPI und Linux-`secret-tool` sind im Tokenstore angebunden.
- [x] Lokale Linux-AES-Speicherung nur nach ausdrücklicher Auswahl gemäß Benutzerentscheidung; keine automatische Umschaltung bei Keyringfehlern. Migration und Dateifehler sind durch Gegenproben abgesichert.
- [x] Quellenaktionen übernehmen gültige Entwürfe und laden laufende Sequenzen neu; ungültige Entwürfe verhindern den Wechsel.
- [x] Gespeicherte leere Seitenlisten ersetzen die Startseite; ohne Workspace-Datei bleibt sie erhalten.
- [x] Transport, Port, HA-Aktivierung und Helligkeit speichern/wiederherstellen; Windows-Import sichtbar und um Hardwarepräferenzen ergänzt.
- [x] Gemeinsame Displayaktionen und sichere Dashboard-Handausgabe an den angewendeten Transport anbinden; Geräte-UI-Tests und Sichtprüfung bei Normal-/Mindestgröße.

### Oberfläche und Verifikation

Zusätzlich zu den portablen Archiven entstehen Debian-Pakete für amd64 und arm64 mit Startmenüeintrag, Anwendungssymbol, Bibliotheksabhängigkeiten und Prüfsumme. Archivstarts für alle drei RIDs sowie Paketinstallation und GUI-Smoke auf Debian 12/13 bestehen in CI. ARM64 läuft nativ auf `ubuntu-24.04-arm`, einschließlich Debian-arm64-Containern. Der Benutzer bestätigt zusätzlich den Debian-Paketstart auf Raspberry Pi 5 mit Debian 13, KDE Plasma 6.3.6 und Wayland-Sitzung für Build 622. Weiterführende Desktop-/Geräteprüfungen bleiben getrennt. Bedienung und Paketbau stehen in [release.md](../release.md).

Die Übersetzungen der zehn Menüseiten einschließlich dynamischer Status-/Fehlertexte und About sind ergänzt und mit Sprachwechseln geprüft. PORT-06A ergänzt Spektrum-/Glyphenvorschau, den Bedingungsbrowser, Sensorauswahl, grafische Medienpegel, Diagnosefelder, Speicherrückmeldungen und konsistente Presets. Das klickbare Pixelraster und die Kurvenpunktbearbeitung verwenden dieselben Textformate; Temperaturen bleiben präzise. Hilfe und fünf Sprachkataloge beschreiben diese Bedienung. Sichtprüfung, PORT-07-Paketnachweise und Plattformgrenzen stehen im [Status](linux-port-status.md); verbleibende Arbeiten in [TODO](../../TODO.md).

PORT-06A-Nachweis: Die Tests prüfen Snapshot → Spektrumvariable → Preset → sichtbare Vorschau → virtuellen Transport, Quellenverlust, Bedingungsfelder/-browser, fünf identische Hardware-Presets, gespeicherte fehlende Sensoren, Glyphensynchronisierung nur bei aktiver Option, Speicherfehler mit erhaltenem Entwurf und präzise Kurvenpunkte. Die betroffenen Ansichten sind in fünf Sprachen bei 950 × 600 gerendert und angesehen. Die Linux-Suite und native Paketstarts stehen im aktuellen Status. Physische LIS2-Ausgabe und reale HA-/Playernachweise bleiben getrennte Zielsystemprüfungen.

- [x] Zehn Hauptansichten, Hilfe und About sind angelegt und angebunden.
- [x] System/Light/Dark sowie System/English/German/French/Turkish/Russian sind auswählbar.
- [x] Variablenbrowser enthält Suche, Kategorien, HA-Filter, Livewerte, persistente Favoriten und Einfügen in beide Zeilen.
- [ ] Semantischen Funktionsabgleich aller zehn Ansichten einschließlich Fehler-, Leer-, Auswahl- und Persistenzpfaden schließen.
- [ ] Hilfe und About mit WPF-Inhaltstiefe und Bedienabläufen abgleichen.
- [x] Hilfesprache einschließlich Systemstandard und offenem Dialog prüfen; Inhalte gegen aktuelle Avalonia-Abläufe korrigieren und Beispiele mit der tatsächlichen Vorlagen-Engine ausführen.
- [x] Avalonia-GUI-Smoke für zehn Ansichten, Hilfe/About, fehlende Medienanbieter, Diagnose und geordnetes Beenden ergänzt; aktuelle Nachweise im Status.
- [ ] Desktop-Darstellung, Tastatur, DPI, Theme und Sprachen auf den Zielsystemen prüfen.
- [x] Linux-Tray/XDG-Autostart, Single Instance mit Fensteraktivierung und Splash anbinden; Windows-Tray erhalten und Wiederherstellen ohne erneuten Geräteverbindungsaufbau prüfen.
- [ ] Native Tray-Bedienung, Autostart bei Anmeldung und Plattformabweichung beim Schließen auf den Ziel-Desktops abnehmen.
- [x] Pakete `win-x64`, `linux-x64` und `linux-arm64` werden selbstständig veröffentlicht und mit SHA-256-Dateien hochgeladen.
- [x] Extrahierte Pakete starten; ARM64 nativ ausführen und Laufzeitabhängigkeiten dokumentieren. CI-Nachweis `39750384348` zu `a6a839f`; manuelle Desktop-/Geräteabnahme bleibt getrennt.
- [x] Support-/Lizenzstatus von .NET/Avalonia anhand offizieller Quellen geprüft; .NET-Supportende, Avalonia-Patchstand und fehlende Distributionshinweise in `release.md`/TODO festgehalten. Die nötigen Umstellungs- und Lizenzarbeiten sind dadurch nicht erledigt.
- [x] WPF-App und deren Testprojekt auf Benutzerauftrag entfernen, Hilfetexte übernehmen und den Protokolltester auf Avalonia portieren. Solution und Releasepfad verwenden Avalonia. Übernahme- und CI-Nachweis stehen im Status.

## Sicherheits- und Abnahmeregeln

Kein physisches LIS2 steht für die bisherigen automatisierten Prüfungen zur Verfügung. Physische Befunde werden niemals aus virtuellen Tests abgeleitet. Hardwareprüfungen bleiben separat ausgewiesen und blockieren keine rein softwareseitigen Arbeitspakete.

Der Benutzer sieht manuelle Desktop-Abnahme nach Umsetzung des Ports vor. Fehlende Desktop-Abnahme darf nicht als erfolgreicher visueller Nachweis erscheinen. Erforderliche technische und gestalterische Prüfungen der jeweils bearbeiteten Oberfläche bleiben ausdrücklich vom späteren Benutzerentscheid zu unterscheiden.

Unbekannte Protokollkommandos werden nicht automatisch gesendet. 19200 Baud/8N1, serialisierte Ausgabe, Sensor-Fail-safe und nicht persistente Lüfterfreigabe bleiben verbindlich. WMP Legacy ist unter Windows über zwei native Plug-ins angebunden. Installation und reale Playerprüfung stehen in [wmp-legacy-integration.md](../wmp-legacy-integration.md).

## Vorgehen je Arbeitspaket

1. Aktuellen Branch-HEAD und zugehörige CI prüfen; fehlgeschlagene CI vor neuer Implementierung bearbeiten.
2. Ein zusammenhängendes Paket aus TODO wählen und Ergebnis, betroffene Schichten und Abnahmekriterien kurz festlegen.
3. WPF-Verhalten, aktuelle Implementierung und vorhandene Tests lesen.
4. Code, Regressionstests und betroffene Dokumentation gemeinsam bearbeiten. Keine spekulativen APIs oder Fehler-Fallbacks ergänzen.
5. Nur aufgabenbezogene Dateien prüfen und committen. Keine Mikro-Pushes zum Ausprobieren unzusammenhängender Teiländerungen.
6. Nach einem autorisierten Push genau dessen CI prüfen und Fehler beheben. Code vorhanden, Tests erfolgreich, Desktop geprüft und Hardware geprüft getrennt ausweisen.

Aktuelle Agenten-, Sandbox- und Benutzeranweisungen gelten zusätzlich. Dieses PRP erteilt keine pauschale Berechtigung zu externen Schreibaktionen. Nächstes Paket und verbleibende fachliche Entscheidungen stehen ausschließlich in [TODO](../../TODO.md).
