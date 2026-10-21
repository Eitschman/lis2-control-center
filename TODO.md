# TODO: Avalonia-Portierung

Stand: 21.10.2026. Branch: `feature/linux-avalonia`.
Implementierungsstand: PORT-06 mit Desktopintegration, benannten Glyphenvariablen und vertiefter Hilfe. Lokal 416 Avalonia-Tests erfolgreich, ein Linux-Schlüsselbundtest unter Windows übersprungen; 50 Displaytests erfolgreich. CI-Nachweise und Zielsystemprüfungen im [Status](docs/prp/linux-port-status.md).

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

Das Übersetzungspaket vor PORT-04 ist umgesetzt und mit isolierten Avalonia-Fenstern geprüft:

- [x] Alle zehn Menüseiten mit Navigation, Beschriftungen, Buttons, Tooltips, Dateidialogtiteln, About sowie Leer-, Status- und Fehlermeldungen abgeglichen. Technische Protokollmeldungen und fremde Ausnahmeinformationen bleiben unverändert.
- [x] Deutsch, Englisch, Französisch, Türkisch, Russisch, Systemstandard und Sprachwechsel geprüft. Statusmeldungen behalten Vorlage und Daten getrennt; Musiktitel, Displayinhalte, Sensorwerte und Schlüssel werden nicht übersetzt. Lüftermodi werden über stabile Indizes gespeichert.
- [x] Katalogabdeckung und Formatplatzhalter geprüft; zehn Ansichten in Deutsch bei 1305 × 893 und 950 × 600 gerendert, zusätzliche französische und russische Ansichten geprüft. Schmale Aktionszeilen umbrechen, der Seiteneditor ordnet Liste und Eigenschaften bei schmalen Fenstern untereinander an. Nachweise und Grenzen stehen im [Status](docs/prp/linux-port-status.md).

- [x] [CI 39565668081](https://github.com/Eitschman/lis2-control-center/actions/runs/39565668081) für `c22048d`: alle sieben Jobs erfolgreich, je OS 359/359 Avalonia-, 55/55 Core-, 48/48 Display- und 12/12 Fans-Tests; beide nativen Smokes und drei Pakete erfolgreich. Linux-Sichtprüfung zusätzlich anhand der Workflow-Screenshots.

Nächstes Paket ist **PORT-07: Pakete und Zielsysteme**. Offene Zielsystem- und Abnahmeprüfungen aus PORT-04 bis PORT-06 bleiben ausdrücklich aufgeführt.

## Weitere Implementierungs- und Prüfaufgaben

### PORT-02: Einstellungen und Gerätebedienung

- [x] Transportmodus, gewählten Port und HA-Aktivierung speichern und wiederherstellen. Fehlender gespeicherter Port bleibt ausgewählt; explizites HA-Aus bleibt auch bei vorhandenem Token erhalten.
- [x] Helligkeitsänderungen speichern und beim Verbinden anwenden, ebenso konfigurierte Glyphen.
- [x] Allgemeine Zeilen-, Clear-, Demo-, ROM-, Glyphen-, Helligkeits- und Seitenaktionen an den angewendeten Transport anbinden. Ein noch nicht angewendeter ComboBox-Wechsel ändert das Ziel nicht.
- [x] Windows-Import sichtbar in Settings; Transport, Port, HA-Aktivierung und Hardwarepräferenzen zusätzlich übernehmen. Token bleibt getrennt; bestehende HA-Verbindung wird beendet und bei Bedarf ausdrücklich neu verbunden.
- [x] Dashboard-Handausgabe mit WPF-Kanalgrenzen und AllowStop, ohne Änderung des gespeicherten Regelmodus. Serielle Freigabe je Aktion, erster Fan-Befehl 100 Prozent und Sicherheitswiederherstellung bei Stop/Trennen. Keine Handausgabe parallel zur automatischen Regelung.
- [x] Regressionen für Persistenz, Import, Transportzuordnung, manuelle Lüfter und Fehlerpfade; tatsächliche Controls, Tastatur-Trennen und gerenderte Dashboard-/Display-/Settings-Ansichten bei Normal- und Mindestgröße. Desktop-/Hardwareprüfungen bleiben getrennt offen.
- [x] [CI 39446107884](https://github.com/Eitschman/lis2-control-center/actions/runs/39446107884) für Code-Commit `ac97be5`: alle sieben Jobs erfolgreich; je OS 326/326 Avalonia-, 55/55 Core-, 48/48 Display- und 12/12 Fans-Tests. Drei Pakete und je 14 gerenderte Ansichten für Windows/Linux hochgeladen.

### PORT-03: Diagnose und Laufzeitlebenszyklus

- [x] Begrenztes UTC-Anwendungs-/Transportjournal einschließlich OK/ERROR, dekodierten Befehlen und aktuellem Textexport.
- [x] Isolierter nativer Desktop-Smoke für Start, zehn Ansichten, Hilfe/About, fehlende Medienanbieter und geordnetes Beenden; Windows lokal und Windows/Linux in CI mit Exit 0, Screenshots und Diagnose vor/nach Shutdown.
- [x] Start-/Stop-/Drain-Pfade abgeglichen: Medienstart vor Dispose drainieren, MPRIS abbrechen/Kindprozess beenden, Timer vor Quellenabbau stoppen, weitere Ressourcen trotz Cleanup-Fehlern abbauen. Verzögerter Start erfolgreich/fehlgeschlagen getestet; reale Quellensysteme separat offen.
- [x] Differenzielle Schreibspannen in serieller und virtueller Session nachgewiesen, einschließlich identischer Frames, manueller Cacheinvalidierung, Reconnect und Teilfehlern.
- [x] Sieben `IPlatform*`-Verträge gegen tatsächliche Aufrufpfade und Zielarchitektur abgeglichen; Abweichungstabelle im Inventar, kein vorgetäuschter Adapterumbau.
- [x] [CI 39455194970](https://github.com/Eitschman/lis2-control-center/actions/runs/39455194970) für `d2d2def`: alle sieben Jobs erfolgreich, je OS 345/345 Avalonia-, 55/55 Core-, 48/48 Display- und 12/12 Fans-Tests; beide nativen Smokes und drei Pakete erfolgreich.

### PORT-04: Medien und Produktgrenzen

- [x] Linux-MPRIS-Position, gespeicherte Playerauswahl, Lifecycle und Fehleranzeige ergänzen. Sekündliche playerctl-Abfrage mit Playerliste, expliziter Instanz, XML-Escaping, Abbruch und Timeout; keine eigene D-Bus-Subscription. Abwesende ausgewählte Player bleiben getrennt und alte Metadaten werden entfernt.
- [ ] VLC und Plexamp einzeln auf realen Zielsystemen prüfen.
- [x] Windows-GSMTC-Paket ohne laufenden Player und mit fehlender Provider-DLL lokal starten. Publish enthält WinRT-Laufzeitdateien; CI prüft Initialisierung, Verbindung, Shutdown und fehlende DLL.
- [ ] GSMTC mit laufendem Windows Media Player auf einem Zielsystem prüfen.
- [x] WMP Legacy bleibt zurückgestellt: Avalonia startet die Quelle nicht, zeigt keine Karte und referenziert das Legacy-Projekt nicht.
- [x] Linux liefert keine VU-/Spektrumanalyse aus MPRIS-Metadaten; Regressionstest prüft diese Grenze. Wine/Winamp bleibt optional.
- [x] Avalonia-Branch-CI baut zusätzlich den selbständigen Windows-Protokolltester und das native Win32/x86-Winamp-Plug-in; eigene Artefakte wie auf main.
- [x] [CI 39569558081](https://github.com/Eitschman/lis2-control-center/actions/runs/39569558081) für `cd4b779`: neun Jobs erfolgreich, je OS 375/375 Avalonia-, 55/55 Core-, 48/48 Display- und 12/12 Fans-Tests; beide nativen Smokes, drei App-Pakete, GSMTC-Paketstart, Protokolltester und Winamp-DLL erfolgreich.

### PORT-05: Home Assistant und Tokenstore

- [x] Lokaler WebSocket-Protokollpartner prüft Anmeldung, Anfangszustände, Ereignisse, Verbindungsabbruch, Reconnect und Deaktivierung; fehlgeschlagene Zustands-/Aboanfragen werden sichtbar, alte Werte entfernt.
- [x] Aliaswerte einschließlich Einheit und Attributen erreichen Seiten und Variablenbrowser. Attributseiten verwenden `.attribute.`; der Browser zeigt Namen und Werte. GUI-Tests prüfen Favoriten, Seitenanlage und Persistenz.
- [x] Benutzerentscheidung: Lokale AES-Speicherung unter Linux nur nach ausdrücklicher Auswahl in der App. Auswahl wird gespeichert; Keyringfehler aktivieren keinen Dateipfad.
- [x] Migration löscht AES-Dateien erst nach erfolgreicher nativer Speicherung und Rückleseprüfung. Gegenproben erhalten Dateien bei Fehlern; beschädigte Dateien, fehlende Schlüssel und offene Unix-Dateirechte werden erkannt.
- [x] Speichern erfolgt vor neuer Verbindung. Deaktivierung stoppt die Quelle und bleibt auch bei fehlgeschlagener Tokenlöschung gespeichert; Fehler erscheinen getrennt vom laufenden Quellenstatus.
- [x] Linux-CI: 400/400 Avalonia-Tests einschließlich echtem isoliertem Secret Service, Prozess-Timeout/Fehler, Unix-Rechten und AES-Auswahl erfolgreich; Windows: 399 erfolgreich, ein Linux-Schlüsselbundtest übersprungen. Code und Lauf im [Status](docs/prp/linux-port-status.md).
- [ ] Mit realer HA-Instanz und dem Desktop-Schlüsselbund auf den Zielsystemen prüfen; lokale WebSocket-Fixtures ersetzen diese Prüfung nicht.

### PORT-06: WPF-Abgleich und Desktopintegration

- [ ] Die zehn Ansichten anhand der [aktuellen Matrix](docs/prp/wpf-avalonia-parity-audit.md) semantisch prüfen, einschließlich Fehler-, Leer-, Auswahl- und Speicherpfaden.
- [x] Zehn Hilfethemen einschließlich Titel, Navigation und Absätzen in fünf Sprachen; gespeicherte Sprache, Systemstandard und Wechsel im offenen Dialog mit Controls geprüft.
- [x] Hilfetexte gegen den Avalonia-Code prüfen: Lüftermodi/-Sicherheitsgrenzen, Medien je Plattform, numerische Glyphenreferenzen, Variablenliste, Formatierung und Sichtbarkeit.
- [x] Benannte Glyphenschlüssel einschließlich Normalisierung, Umbenennen, doppelten Namen und geschützten numerischen Slots anbinden; Hilfe und Editor zeigen ausführbare Schlüssel.
- [x] Statische Clock-/Winamp-/WindowsMedia-/MPRIS-Variablen auch ohne Quelle beschreiben und WPF-Fachübersetzungen gemeinsam verwenden.
- [x] Hilfe neben dem Bereichstitel und per F1 öffnen; Tray-Menütexte folgen fünf Sprachen.
- [x] Zehn Ansichten in fünf Sprachen mit Hell/Dunkel/System bei 950 × 600, Titelgrenzen, Fokus und F1 prüfen; abgeschnittene Port-/Glyphen-/Lüftertexte korrigieren.
- [ ] Tatsächlichen OS-Themewechsel, Monitor-/DPI-Wechsel und native Tastaturbedienung auf den Ziel-Desktops prüfen.
- [x] Display-Formatierung in en-US, de-DE und fr-FR prüfen; Produktformatierung nach `CurrentCulture` erhalten. Lokaler Lauf: 50/50.
- [ ] Zusätzliche Bedienflächen und nicht ausdrücklich geforderte Titelzusätze im bearbeiteten UI-Bereich bereinigen; genehmigten Variablenbrowser erhalten.
- [x] Windows-/Linux-Tray, XDG-Autostart, Single Instance mit Fensteraktivierung und Splash anbinden. Wiederanzeigen startet die Geräteverbindung nicht erneut; Gegenprobe und Regression vorhanden.
- [ ] Tray-Menübedienung und Anmeldung mit Autostart auf realen Windows-/Linux-Desktops prüfen. Linux minimiert in die Taskleiste und beendet beim Schließen; Windows minimiert/schließt in den Infobereich. Plattformabweichung bei der Benutzerabnahme prüfen.
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
