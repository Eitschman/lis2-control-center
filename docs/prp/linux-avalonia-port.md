# PRP: LIS2 Control Center mit Avalonia

Stand: 21.10.2026. Arbeitsbranch: `feature/linux-avalonia`.
Implementierungsstand: PORT-06; genaue Prüfnachweise im [Status](linux-port-status.md).

## Ziel und verbindlicher Umfang

Eine gemeinsame Avalonia-Anwendung ersetzt die WPF-Anwendung auf Windows x64 und unterstützt Linux x64 sowie Linux ARM64. Die vorhandene WPF-Anwendung ist während der Portierung die Referenz für Funktionen, Anordnung, Interaktionen, Einstellungen und Vorlagen. Sie bleibt bis zum nachgewiesenen Abgleich und zur Benutzerabnahme erhalten. Keine dauerhafte zweite Windows-Oberfläche und kein WPF-Runtime-Adapter.

Die zehn Hauptansichten bleiben Dashboard, Display, Pages, Media, Events, Hardware, Fan Control, Settings, Diagnostics und Home Assistant. Windows behält Winamp und GSMTC; Linux erhält die passenden Sensor- und Medienanbieter. Home Assistant bleibt lesend. Eigene OctoPrint-, Plex- oder Docker-Datenquellen sind für bereits über HA verfügbare Werte nicht vorgesehen.

Die genehmigte UI-Abweichung ist der modale, durchsuchbare Variablenbrowser anstelle der großen WPF-ComboBox. Lüfterfreigabe und Sicherheitsgrenzen bleiben unabhängig von einer äußerlich gleichen Oberfläche verbindlich. Weitere Abweichungen werden im [Paritätsaudit](wpf-avalonia-parity-audit.md) erfasst, ohne sie dadurch zu genehmigen.

## Arbeitsgrundlage und Nachweis

- [Implementierungsstatus](linux-port-status.md): aktueller Codebefund und CI-Nachweis.
- [Inventar](linux-port-inventory.md): Projekte, Zuständigkeiten, Plattformgrenzen und Tests.
- [Paritätsaudit](wpf-avalonia-parity-audit.md): vorhandene Ansichten und offene Unterschiede.
- [TODO](../../TODO.md): priorisierte Restarbeiten und nächstes Arbeitspaket.
- [Architektur](../architecture.md), [Protokoll](../protocol.md) und [Release-Verfahren](../release.md).

Builds, Tests, nativer Desktop-Smoke und Pakete werden über `.github/workflows/linux-portable.yml` geprüft. Lokal bestehen 399 Avalonia-Tests; der zusätzliche Linux-Schlüsselbundtest ist unter Windows übersprungen. HA-Verbindungsabläufe verwenden einen lokalen WebSocket-Protokollpartner. Linux-CI führt die Suite mit isoliertem echtem Secret Service aus; der aktuelle Nachweis steht im [Status](linux-port-status.md). Dieser ordnet CI-Läufe dem jeweiligen Code-Commit zu.

## Implementierungsstand

Abgehakte Punkte bezeichnen vorhandenen Code mit dem angegebenen Testumfang. Sie bedeuten weder Desktop-Abnahme noch physische Geräteprüfung.

| Bereich | Stand | Offener Umfang |
| --- | --- | --- |
| Projektstruktur | Avalonia 11.3.6, .NET 8, portable Bibliotheken und `LIS2.Runtime` vorhanden | Plattformverträge sind noch nicht durchgängig angebunden; UI-Code enthält weitere Orchestrierung |
| Display-Runtime | Scheduler, Scrolling, Quellen, Ereignisse und getrennte virtuelle/serielle Sequenzen; gemeinsame Geräteaktionen und differenzielle Spannen am angewendeten Transport | Physische Ausgabe offen |
| Seiteneditor | Seitenpflege, Vorlagen, Bedingungen, Presets, animierte Vorschau, Variablenbrowser, validierter JSON-Import/-Export und gemeinsamer Entwurfsschutz für Quellenaktionen vorhanden | GUI-Interaktionen und WPF-Bedienparität prüfen |
| Glyphen | Acht 5x8-Slots, Dateien, Namen, Spektrumset und Ersetzungsdialog vorhanden | Physische Zeichenprüfung und UI-/Transportabgleich offen |
| Sensoren | LibreHardwareMonitor auf Windows; procfs/sysfs/hwmon auf Linux; Aliase, Favoriten und fehlende Sensoren berücksichtigt | Stabile Identitäten und Rechte auf Zielsystemen prüfen |
| Lüfter | Vier Modi plus Off, Kurven, Hysterese, Fail-safe, Simulation, freizugebende serielle Regelung und Dashboard-Handausgabe mit Kanalgrenzen | Weitere WPF-Interaktionen abgleichen; Hardware bleibt ungeprüft |
| Medien | Winamp/GSMTC auf Windows; Linux-playerctl mit gespeicherter Auswahl, Spielposition, Lifecycle und Fehleranzeige; Legacy deaktiviert | Reale VLC-/Plexamp-/Windows-Media-Player-Sitzungen offen |
| Home Assistant | WebSocket-Quelle, Reconnect und Stop, Entitäten, Attributwerte, wirksame Aliase, Favoriten und Seitenaktionen; ausdrückliche lokale Linux-Tokenauswahl | Reale HA-Instanz und Desktop-Schlüsselbund auf Zielsystemen prüfen |
| Einstellungen | Workspace einschließlich Transport/Port/HA-Aktivierung, Helligkeitsanwendung, sichtbarer Windows-Import, Theme/Sprache, Windows-/Linux-Tray und plattformeigener Autostart | Nativer Dateidialog, Tray-Bedienung und Anmeldung auf Zielsystemen offen |
| Hilfe und About | Kontextbezogene Hilfe mit zehn Themen in fünf Sprachen, Live-Schlüsseln, statischen Variablenbeschreibungen und numerischen/benannten Glyphen; F1 und Hilfe am Titel; About folgt der Sprache | Native Desktop-Bedienung und Benutzerabnahme offen |
| Diagnose | Zustandsübersicht, virtuelle Zustände, dekodierte Historie und Export mit zeitgestempeltem Anwendungs-/Transportjournal | Sitzungsprotokoll begrenzt; kein dauerhaftes Logarchiv; nativen Dateidialog prüfen |
| CI und Pakete | Windows-/Ubuntu-Builds, vier Testprojekte je OS, nativer Desktop-Smoke und drei selbstständige Pakete mit Prüfsummen | Archivstart und echte ARM64-Ausführung fehlen |

## Prüffähige Checkliste

### Portable Laufzeit und Gerätezugriff

- [x] `LIS2.Core`, `LIS2.Display`, `LIS2.Fans`, `LIS2.Sources`, `LIS2.Winamp`, `LIS2.WmpLegacy` und `LIS2.Runtime` verwenden `net8.0`.
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

Die Übersetzungen der zehn Menüseiten einschließlich dynamischer Status-/Fehlertexte und About sind ergänzt und mit Sprachwechseln geprüft. Statische Texte, Formatargumente und unveränderte Nutzdaten werden automatisiert geprüft; die Sichtprüfung und Plattformgrenzen stehen im Status. HA-Verbindung, Attribute, Aliase und Tokenspeicherung sind in PORT-05 mit isolierten Daten geprüft; reale HA-/Playernachweise bleiben offen. Nächstes Paket ist PORT-07 gemäß [TODO](../../TODO.md).

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
- [ ] Extrahierte Pakete starten; ARM64 auf ARM64 ausführen und Laufzeitabhängigkeiten dokumentieren.
- [ ] Support-/Lizenzstatus der verwendeten .NET-/Avalonia-Versionen vor dem Release anhand offizieller Quellen verifizieren.
- [ ] WPF-Projekt und WPF-Releasepfad erst nach Paritätsnachweis und Benutzerabnahme entfernen.

## Sicherheits- und Abnahmeregeln

Kein physisches LIS2 steht für die bisherigen automatisierten Prüfungen zur Verfügung. Physische Befunde werden niemals aus virtuellen Tests abgeleitet. Hardwareprüfungen bleiben separat ausgewiesen und blockieren keine rein softwareseitigen Arbeitspakete.

Der Benutzer sieht manuelle Desktop-Abnahme nach Umsetzung des Ports vor. Fehlende Desktop-Abnahme darf nicht als erfolgreicher visueller Nachweis erscheinen. Erforderliche technische und gestalterische Prüfungen der jeweils bearbeiteten Oberfläche bleiben ausdrücklich vom späteren Benutzerentscheid zu unterscheiden.

Unbekannte Protokollkommandos werden nicht automatisch gesendet. 19200 Baud/8N1, serialisierte Ausgabe, Sensor-Fail-safe und nicht persistente Lüfterfreigabe bleiben verbindlich. WMP Legacy ist als Produktentscheidung zurückgestellt und wird von Avalonia nicht gestartet.

## Vorgehen je Arbeitspaket

1. Aktuellen Branch-HEAD und zugehörige CI prüfen; fehlgeschlagene CI vor neuer Implementierung bearbeiten.
2. Ein zusammenhängendes Paket aus TODO wählen und Ergebnis, betroffene Schichten und Abnahmekriterien kurz festlegen.
3. WPF-Verhalten, aktuelle Implementierung und vorhandene Tests lesen.
4. Code, Regressionstests und betroffene Dokumentation gemeinsam bearbeiten. Keine spekulativen APIs oder Fehler-Fallbacks ergänzen.
5. Nur aufgabenbezogene Dateien prüfen und committen. Keine Mikro-Pushes zum Ausprobieren unzusammenhängender Teiländerungen.
6. Nach einem autorisierten Push genau dessen CI prüfen und Fehler beheben. Code vorhanden, Tests erfolgreich, Desktop geprüft und Hardware geprüft getrennt ausweisen.

Aktuelle Agenten-, Sandbox- und Benutzeranweisungen gelten zusätzlich. Dieses PRP erteilt keine pauschale Berechtigung zu externen Schreibaktionen. Nächstes Paket und verbleibende fachliche Entscheidungen stehen ausschließlich in [TODO](../../TODO.md).
