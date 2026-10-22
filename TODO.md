# TODO: Avalonia-Portierung

Stand: 22.10.2026. Branch: `feature/linux-avalonia`.
Implementierungsstand: PORT-06B mit laufender Lüfterkonfiguration, erhaltener Seitenrotation bei Helligkeit/Glyphen, sichtbaren Importfehlern und aktueller Bedingungsanzeige. Lokal 451 Avalonia-Tests erfolgreich, ein Linux-Schlüsselbundtest unter Windows übersprungen. Nativer Windows-Smoke mit Exit 0. CI-Nachweise und Zielsystemprüfungen im [Status](docs/prp/linux-port-status.md).

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

Die automatisierten Paketprüfungen aus **PORT-07** sind nachgewiesen, einschließlich nativer ARM64-Ausführung. Als nächste Softwarearbeit stehen Support-/Versionspflege und Distributionslizenzen an. Die softwareseitigen Punkte A01 bis A14 aus PORT-06A sind umgesetzt. Offene Zielsystem- und Abnahmeprüfungen aus PORT-04 bis PORT-07 bleiben ausdrücklich aufgeführt.

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

Technischer Nachweis: [CI 39581947442](https://github.com/Eitschman/lis2-control-center/actions/runs/39581947442) für `35a2df0`, neun erfolgreiche Jobs. Linux 417/417 Avalonia-Fälle, Windows 416 erfolgreich und ein übersprungener Linux-Fall; je OS Core 55/55, Display 50/50 und Fans 12/12. Native Smokes einschließlich minimiertem Start, zweitem Prozess und Linux bei 150 Prozent; drei App-Pakete, Protokolltester und Winamp-DLL geprüft. Die offenen manuellen Abnahmepunkte bleiben unten erfasst.

- [ ] Die zehn Ansichten anhand der [aktuellen Matrix](docs/prp/wpf-avalonia-parity-audit.md) semantisch prüfen, einschließlich Fehler-, Leer-, Auswahl- und Speicherpfaden.
- [x] Zehn Hilfethemen einschließlich Titel, Navigation und Absätzen in fünf Sprachen; gespeicherte Sprache, Systemstandard und Wechsel im offenen Dialog mit Controls geprüft.
- [x] Hilfetexte gegen den Avalonia-Code prüfen: Lüftermodi/-Sicherheitsgrenzen, Medien je Plattform, numerische Glyphenreferenzen, Variablenliste, Formatierung und Sichtbarkeit.
- [x] Benannte Glyphenschlüssel einschließlich Normalisierung, Umbenennen, doppelten Namen und geschützten numerischen Slots anbinden; Hilfe und Editor zeigen ausführbare Schlüssel.
- [x] Statische Clock-/Winamp-/WindowsMedia-/MPRIS-Variablen auch ohne Quelle beschreiben und WPF-Fachübersetzungen gemeinsam verwenden.
- [x] Hilfe neben dem Bereichstitel und per F1 öffnen; Tray-Menütexte folgen fünf Sprachen.
- [x] Zehn Ansichten in fünf Sprachen mit Hell/Dunkel/System bei 950 × 600, Titelgrenzen, Fokus und F1 prüfen; abgeschnittene Port-/Glyphen-/Lüftertexte korrigieren.
- [ ] Tatsächlichen OS-Themewechsel, Monitor-/DPI-Wechsel und native Tastaturbedienung auf den Ziel-Desktops prüfen.
- [x] Display-Formatierung in en-US, de-DE und fr-FR prüfen; Produktformatierung nach `CurrentCulture` erhalten. Lokaler Lauf: 50/50.
- [x] Redundante zusätzliche Seitenüberschrift entfernen und genehmigten Variablenbrowser erhalten.
- [ ] Zusätzliche Start-/Stop-, Datei- und Testaktionen gegenüber WPF im manuellen Bedienungsabgleich bewerten.
- [x] Windows-/Linux-Tray, XDG-Autostart, Single Instance mit Fensteraktivierung und Splash anbinden. Wiederanzeigen startet die Geräteverbindung nicht erneut; Gegenprobe und Regression vorhanden.
- [ ] Tray-Menübedienung und Anmeldung mit Autostart auf realen Windows-/Linux-Desktops prüfen. Linux minimiert in die Taskleiste und beendet beim Schließen; Windows minimiert/schließt in den Infobereich. Plattformabweichung bei der Benutzerabnahme prüfen.
- [x] WPF-App und Testprojekt auf ausdrücklichen Benutzerauftrag entfernen; Protokolltester auf Avalonia portieren. Offene Zielsystemprüfungen bleiben separat erhalten.

### PORT-06A: Paritätskorrekturen und Bedienung

A01 bis A14 sind softwareseitig umgesetzt und Teil der 451 erfolgreichen Avalonia-Tests des aktuellen PORT-06B-Stands; ein Linux-Schlüsselbundtest ist unter Windows übersprungen. Aktueller CI-Lauf sowie Zielsystemprüfungen stehen im [Status](docs/prp/linux-port-status.md). Die WPF-Referenz liegt in Git-Commit `922ca1b`; im aktuellen Quellbaum verwenden App und Protokolltester Avalonia.

- [x] **A01: Winamp-Spektrum.** `Winamp.SpectrumGlyphs` verwendet den aktiven Spektrumzeichensatz, sonst das Textspektrum. Quelle, Preset, Vorschau und virtuelles Gerät werden mit 0..15-/0..255-Pegeln und Quellenverlust geprüft.
- [x] **A02: Glyphenvorschau.** Gemeinsame Abbildung in Dashboard und Seiteneditor: Slotmarkierungen für eigene Zeichen, Balken für Spektrumstufen. Der Gerätepfad behält die privaten VFD-Zeichen.
- [x] **A03: Bedingungseditor.** Rohtext und Seitenwechsel befüllen Schlüssel, Operator und Wert. Der genehmigte Variablenbrowser übernimmt aktuelle Schlüssel; ungültige Ausdrücke bleiben bearbeitbar.
- [x] **A04: Sensorauswahl.** Direkte Liste mit Namen, Wert und Einheit im Lüftereditor; manuelle und gespeicherte fehlende Schlüssel bleiben erhalten. Physische Freigabe bleibt ausdrücklich erforderlich.
- [x] **A05: Medien.** Grafische Stereopegel und zwanzig Spektrumbänder, Pipe und letzte Snapshotzeit; sichtbare Windows-Medienquelle und Wiedergabe. Quellenverlust leert Metadaten und Pegel.
- [x] **A06: Diagnose.** Version, UTC-Zeit, angewendeter Transport/Port, gesendeter Frame, Quellenfehler und vier Lüfterkonfigurationen im Bericht. Virtueller Zustand separat; dauerhafte Start-/Absturzspuren ohne Ausnahmemeldungen und Tokens in `avalonia-errors.log`.
- [x] **A07: Glyphensatzwechsel.** Geladene Datei-/Beispielsätze werden nach erfolgreichem Speichern bei aktivierter Glyphenoption an den verbundenen angewendeten Transport gesendet. Gespeichert und gesendet werden getrennt gemeldet; ohne Option keine zusätzliche Programmierung.
- [x] **A08: Hardware-Presets.** Beide Einstiege verwenden denselben Sensorkatalog, dieselben Favoriten/Aliase und Einheiten. Fünf Presetabläufe werden mit derselben Sensor-Fixture verglichen.
- [x] **A09: Plattformumfang.** Windows-Vorlagen und Linux-MPRIS-Vorlage passend zur Plattform; bestehende/importierte Seiten bleiben erhalten. Lüfterquellenbeschriftung folgt dem Betriebssystem.
- [x] **A10: Home Assistant.** Entitäts-/Attributschlüssel per Doppelklick und Strg+C; fokussierbare Listen. Attributkopie mit tatsächlichen Maus- und Tastatureingaben gegen lokalen WebSocket-Protokollpartner geprüft.
- [x] **A11: Speicherstatus.** Seiten, Glyphen, Sensor- und HA-Präferenzen zeigen geändert, speichern, gespeichert oder Fehler am Editor. Erfolg folgt dem Schreibabschluss; ältere Abschlüsse bestätigen keine neueren Eingaben. Blockierte Workspace-Schreibdatei hält Seiten-/Glyphenentwürfe und lässt erneutes Speichern zu.
- [x] **A12: Lüfterbedienung.** Reguläre Konfiguration vor den aufklappbaren Prüfwerkzeugen; physische Freigabe bleibt getrennt und nicht persistiert.
- [x] **A13: Editoren.** Klickbares 5×8-Raster mit Leertastenbedienung; Kurvenpunkte anlegen, ändern und löschen. Beide schreiben in die bestehenden Textformate; Dezimaltemperaturen bleiben ohne Rundung erhalten, Ausgaben bleiben ganzzahlig bei 0..100.
- [x] **A14: Nachweise.** Durchgängige Spektrumregression, Bedingungsbrowser, Speicherfehler, Glyphenoption, Presetabgleich und Quellenverlust. Seitenbedingungen, Pixelraster, Kurvenpunkte, Medienpegel und Diagnose mit Skia bei 950 × 600 in Englisch, Deutsch, Französisch, Türkisch und Russisch gerendert und angesehen. Hilfe, Audit und Arbeitsdokumente beschreiben diese Abläufe.

Neue HA-Parallelquellen, WMP Legacy, allgemeiner Settings-Import/-Export, Standardwerte-Wiederherstellung und Einrichtungsassistent sind nicht Teil dieses Pakets. Der vorhandene Windows-Import bleibt erhalten.

### PORT-06B: Laufzeit- und Speicherparität

- [x] **B01: Laufende Lüfterkonfiguration.** Gespeicherte Modi, Werte, Sensoren, Grenzen und Kurven gelten ab dem nächsten Regeltakt für serielle/virtuelle Regelung und den kontinuierlichen Dry-run. Speichern startet keine Ausgabe und erneuert keine Freigabe.
- [x] **B02: Helligkeit und Glyphen.** Geräteänderungen warten auf laufende Frames. Zeitplan, Scrollzustand, aktive Ereignisse und laufende Timer bleiben erhalten. Gestoppte Sequenzen starten dadurch nicht; manuelle Bildausgaben stoppen weiterhin die Rotation.
- [x] **B03: Seitenimport.** Ein Fehler beim Speichern des Workspace bleibt als Fehler sichtbar. Erfolg wird erst nach dem Schreibabschluss gemeldet; die vorhandene Datei bleibt bei Schreibfehlern erhalten.
- [x] **B04: Bedingungsanzeige.** Der Seiteneditor bewertet die Sichtbarkeitsbedingung mit den aktuellen Quelldaten und aktualisiert die Meldung beim Sprachwechsel.
- [x] **Nachweise.** Zwölf Regressionen zu beiden Transportpfaden, Kanalgrenzen und Fail-safe, unveränderter Freigabe, Dry-run, Rotation/Ereignissen, exklusiver Ausgabe sowie gültigem/fehlgeschlagenem Import. Windows-Suite: 451 erfolgreich, ein Linux-Fall übersprungen. Bedingungsanzeige und Importfehler bei 950 × 600 gerendert und angesehen; nativer Windows-Smoke mit Exit 0.

### PORT-07: Pakete und Zielsysteme

- [x] Zusätzliche Debian-Pakete für amd64 und arm64 mit Startmenüeintrag, Symbol, Bibliotheksabhängigkeiten und SHA-256 in den Avalonia-Workflow integrieren.
- [x] amd64-/arm64-Pakete in Debian 12/13 installieren, nativen GUI-Smoke als gewöhnlicher Benutzer und Deinstallation mit erhaltenen Benutzerdaten prüfen.
- [x] Extrahierte Windows-/Linux-Pakete starten: SHA-256, frische Extraktion, nativer Apphost und GUI-Smoke für alle drei RIDs; Ergebnis, Screenshots und Laufzeitumgebung als CI-Artefakt.
- [x] Linux ARM64 nativ auf `ubuntu-24.04-arm` prüfen; zusätzlich Debian 12/13 arm64 installieren, GUI/Keyring prüfen und deinstallieren. CI `39750384348` zu `a6a839f`, alle neun Jobs erfolgreich.
- [x] Benutzer bestätigt Debian-Paketstart auf Raspberry Pi 5 mit 16 GB RAM: Debian 13 Trixie, ARM64/aarch64, KDE Plasma 6.3.6, Wayland-Sitzung, `0.0~ci622.1` aus CI-Build 622 zu `c1b74ea`.
- [ ] DPI, tatsächlich verwendeten App-Displaybackend und Abhängigkeiten auf dem Pi protokollieren; weiterführende Desktopprüfungen übernimmt der Benutzer anhand der Liste in `docs/release.md`.
- [x] X11-/DBus-/Keyring-Umgebungen, Runtimeversion und native Bibliotheksauflösung für automatisierte Paketstarts protokollieren. Positive und negative Paketprüfungen sowie native ARM64-Ausführung nachgewiesen.
- [ ] GTK-Dateidialoge, Wayland/XWayland, Anmeldung/Tray und serielle Zugriffsrechte auf den tatsächlichen Benutzer-Desktops prüfen; manuelle Prüfungen übernimmt der Benutzer.
- [x] Support- und Lizenzstatus von .NET/Avalonia anhand offizieller Quellen prüfen: .NET 8.0.32, Supportende 10.11.2026; Avalonia 11.3.6 unter MIT, neuere Patches vorhanden. Befunde und Quellen in `docs/release.md`.
- [ ] Vor Avalonia-Release ein eigenes Paket für unterstütztes .NET und Avalonia-Patchstand einplanen und prüfen.
- [ ] Windows-Runtimepatch angleichen: geprüfte Pakete enthalten Windows .NET 8.0.31 gegenüber Linux 8.0.32.
- [ ] Lizenztexte/Fremdkomponentenhinweise und nötige Quellverweise zu den tatsächlich verteilten NuGet-Paketen in Archive und Debian-Pakete aufnehmen; MPL-2.0-Pakete, HidSharp und Mono.Posix gesondert abgleichen.
- [x] Hauptbranch- und Tag-Workflows auf denselben Avalonia-Paketbau umstellen. Tag-Version an App, Protokolltester und Debian-Pakete übergeben; kein Release-Tag angelegt.
- [ ] Main-Übernahme und ersten gemeinsamen CI-Lauf nachweisen.

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
