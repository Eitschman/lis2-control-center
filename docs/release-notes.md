# LIS2 Control Center 0.1.23

## Gezielte Verbindungstests

ROAD-07 ergänzt unter Diagnose eine plattformgerechte Quellenauswahl mit einzelnen Prüfschritten, Ergebnissen und passenden Handlungsmöglichkeiten. Home Assistant prüft Adresse, Token, WebSocket-Verbindung, Begrüßung und Anmeldung über eine separate Testverbindung. Bestehende Quellen und Tokenstore bleiben unverändert. Hardware, Medien und Transport verwenden ihre vorhandenen Zustände; Metadaten und Audio werden getrennt geprüft. Ohne Wiedergabe werden Audioprüfungen übersprungen.

Serielle Tests prüfen Portauswahl, Betriebssystemerkennung und eine bereits geöffnete Verbindung, ohne einen Port zu öffnen oder Befehle zu senden. Ein erkannter Port allein bestätigt weder LIS2-Identität noch Zugriffsrechte. Tests sind abbrechbar und werden beim Shutdown abgebrochen und abgewartet. Zeitpunkt und Ergebnisse stehen im Diagnoseexport. Zugangsdaten, URLs und fremde Serverfehlertexte erscheinen nicht in den Testergebnissen. Hilfe und Bedienung sind fünfsprachig. [Bedienung und Grenzen](connection-tests.md).

## Sensor- und Lüfterverlauf

ROAD-06 ergänzt die Verlaufssicht in der Lüftersteuerung. Regelsensor, berechneter Sollwert und gemessene Drehzahl verwenden dieselbe Zeitachse mit getrennten Skalen. 5, 15 oder 60 Minuten sind auswählbar. Fehlende Messungen und nicht zugeordnete Drehzahlsensoren bleiben erkennbar. Der Sollwert verwendet gespeicherte Einstellungen einschließlich Fail-safe und bestätigt keine physische Ausgabe.

Die letzten 60 Minuten bleiben ausschließlich im Arbeitsspeicher, höchstens 1801 Punkte je Kanal. Neustart oder eine geänderte Kanalkonfiguration beginnen einen neuen Verlauf. Hardwarefreigaben werden nicht erweitert. Bedienung und Hilfe sind in fünf Sprachen ergänzt. [Bedienung und Grenzen](fan-history.md).

## Vorlagenprüfung und Datenaktualität

ROAD-03 ergänzt anklickbare Hinweise direkt unter den beiden Vorlagenzeilen. Fehlerhafte Operationen, Nachkommastellen, Klammern, unbekannte statische Variablen und fehlende Glyphen werden von derzeit fehlenden Werten und nicht entdeckten dynamischen Schlüsseln unterschieden. Anklicken markiert die betroffene Stelle; Korrekturen aktualisieren die Vorschau. Speichern bleibt möglich.

ROAD-04 ergänzt Zustand, lokalen Zeitpunkt und Datenalter bei Hardware, ausgewähltem Sensor, Home Assistant und Medien. Fehlgeschlagene oder fehlende Erfassungen verjüngen den Zeitpunkt nicht. Periodische Hardware und Medien haben Zeitgrenzen; ereignisgesteuerter Windows Media Player und Home Assistant werden wegen unveränderter Werte allein nicht als veraltet eingestuft. HA nennt ausdrücklich den letzten Quell-Datenempfang. Bedienung und Hilfe sind in fünf Sprachen ergänzt. [Details und Grenzen](template-validation-and-freshness.md).

Das Release lässt sich über Einstellungen → Updates beziehen. Der Benutzer bestätigt den Pi-Updateversuch von v0.1.19 auf v0.1.21 als erfolgreich. Der Benutzer bestätigt am 30.10.2026 auch den Windows-Updater als erfolgreich; Ausgangs- und Zielversion sind nicht angegeben. Der Paket-Smoke prüft Austausch und Wiederherstellung zusätzlich automatisiert.

## Updates für Windows und Linux

Die Einstellungen bieten manuelle und optionale tägliche Suche nach stabilen GitHub-Releases, Versionsvergleich, Versionshinweise und plattformgerechte Downloads. SHA-256-Prüfung und offizielle Asset-URLs sind Voraussetzung für die Übergabe. Automatische Prüfung und automatische Windows-Installation sind getrennte, zunächst ausgeschaltete Auswahlen.

Windows bereitet das portable x64-Paket vor, speichert den Workspace und beendet Display-, Audio- und Lüfterlaufzeit geordnet. Ein separater Prozess sichert den bisherigen Installationsordner, tauscht ihn aus und startet die Anwendung neu. Ohne Startbestätigung wird die vorherige Installation wiederhergestellt. Der übergeordnete Ordner muss beschreibbar sein; Player-Plug-ins in anderen Installationsordnern werden nicht ausgetauscht.

Linux lädt das amd64-/ARM64-Debian-Paket und öffnet es mit der eingerichteten System-Paketinstallation. Installation und Rechteabfrage erfolgen dort; anschließend LIS2 neu starten. Dafür werden xdg-utils und eine passende Dateizuordnung benötigt. Portable Linux-Archive werden zum manuellen Entpacken angeboten. GitHub und das Repository sind die Vertrauensquelle; die Prüfsumme aus demselben Release ist keine unabhängige digitale Signatur. [Bedienung, Grenzen und Wiederherstellung](application-updates.md).

Der Benutzer bestätigt den Pi-Updateversuch von v0.1.19 auf v0.1.21 als erfolgreich. Der echte Windows-Prozesswechsel und die Wiederherstellung nach absichtlich fehlendem Startprogramm sind mit isolierten Anwendungskopien geprüft. Hilfe und Bedienung sind in fünf Sprachen ergänzt.

## Linux-VU, Spektrum und plattformgerechte Vorlagen

Der gewählte MPRIS-Player liefert Stereo-VU und zwanzig Spektrumbänder über PipeWire, mit ungefähr 100 ms Aktualisierung in Medienkarte und Displayvariablen. Die Zuordnung berücksichtigt native Programme, Pulse-Clients und Flatpak mit D-Bus-Proxy und getrennten Prozessnummern. Mikrofon und Systemton werden nicht als Ersatzquelle verwendet. Fehlende oder mehrdeutige Streams haben einen eigenen Audiostatus; Metadaten bleiben unabhängig verfügbar.

Linux zeigt ausschließlich die passenden MPRIS-Medienvorlagen einschließlich VU und Spectrum. Windows zeigt stattdessen Winamp, Windows Media Player und Legacy. Gemeinsame Uhr-/Hardwarevorlagen bleiben erhalten. Die Hilfe beschreibt Einrichtung und Variablen in fünf Sprachen.

Benötigt werden eine laufende PipeWire-Desktopsitzung mit Sitzungsmanager sowie `playerctl`, `pipewire-bin` und `dbus-bin`. Die Debian-Pakete empfehlen diese Werkzeuge. Einzelheiten stehen in [Linux-Spieleraudio](linux-media-audio.md).

Der Benutzer bestätigt den erfolgreichen Betrieb mit Tuner als Flatpak auf Raspberry Pi 5. Die Release-CI prüft native, Pulse- und Sandbox-Audiofälle sowie ARM64-Pakete. Weitere konkrete Zielsystem-Gegenproben sind im Leitfaden aufgeführt.

## Einheitliches Schriftbild der Hilfe

Erklärende Absätze verwenden in allen zehn Hilfebereichen die normale Anwendungsschrift, auch wenn sie eingebettete Vorlagenausdrücke enthalten. Nur ausdrücklich gekennzeichnete Codebeispiele verwenden Festbreitenschrift. Die Darstellung wird in Deutsch, Englisch, Französisch, Türkisch und Russisch sowie mit der Systemeinstellung geprüft. Vorlagen, Hilfetexte und Sprachwechsel bleiben erhalten.

## Dashboard-Feinschliff

Numerische Home-Assistant-Zustände werden in Dashboard- und Displayvorlagen auch ohne ausdrückliches Zahlenformat nach den Regionseinstellungen dargestellt, einschließlich Alias- und Ersatzvariablen. `21.50` wird bei deutschen Einstellungen zu `21,50`, ohne Rundung oder Präzisionsverlust. Rohzustände, Sichtbarkeitsbedingungen, Namen und Textattribute bleiben unverändert. Ausdrückliche Zahlenformate bestimmen weiterhin die Nachkommastellen; numerische JSON-Attribute folgen bereits den Regionseinstellungen.

Der Quellenabgleich umfasst Windows-/Linux-Hardware, Uhr, Winamp, WMP Legacy, modernes WMP/GSMTC, Linux-MPRIS und Home Assistant. Hardwareliste, HA-Zustands-/Attributliste, Variablenbrowser und gelesene Lüftersensoren verwenden ebenfalls regionale Zahlenformate. Numerische Medienfelder bleiben Zahlen; Titel, Versionszeichenfolgen, Datums-/Laufzeitformate, Rohdaten und technische Eingabeformate werden nicht umgedeutet. Maßgeblich sind die Regionseinstellungen des Betriebssystems, unabhängig von der Oberflächensprache.

Unbelegte Livewerte zeigen ein dezentes „—“. Konfigurierte Vorlagen ohne Ergebnis zeigen „Nicht verfügbar“ in kleinerer Schrift. Die Lüfterkarte ist niedriger und beschriftet Drehzahlwerte ausdrücklich; nicht zugeordnete Drehzahlsensoren zeigen „Drehzahl: —“. Beide Zahnradschaltflächen haben dieselbe Größe und sichtbare Hover- und Fokuszustände.

Im Livewerte-Dialog passt „Zahlen kompakt“ ausdrückliche Zahlenformate auf höchstens eine Nachkommastelle an und ergänzt Abstände vor unterstützten Einheiten. Ganze Zahlen ohne Nachkommastellen bleiben ganzzahlig. Die Aktion verändert nur den Entwurf; Speichern übernimmt die Änderungen, Abbrechen verwirft sie. Bestehende Vorlagen werden nicht automatisch umgeschrieben. Hilfe und Bedienung sind in fünf Sprachen ergänzt.

## Acht Livewerte und Windows-CPU-Temperaturen

Das Dashboard zeigt acht konfigurierbare Livewerte in vier beziehungsweise zwei Spalten. Bestehende Konfigurationen mit vier Werten behalten Beschriftungen und Vorlagen. Der Editor und die Hilfe unterstützen alle acht Werte in fünf Sprachen.

Fehlende CPU-Temperaturen erzeugen einen sichtbaren lokalisierten Hinweis oberhalb der Hardwareliste, auch wenn GPU- und Lastwerte vorhanden sind. Für CPU-Sensoren verwendet LibreHardwareMonitor den offiziellen signierten PawnIO-Treiber. Eine ausdrücklich gewählte Freigabe für ein konkretes Windows-Benutzerkonto ermöglicht den Betrieb ohne erhöhte Anwendung und ohne eigenen LIS2-Dienst. Die Freigabe gilt für alle Programme dieses Kontos und ist nicht auf Temperaturabfragen begrenzt.

Das Windows-Paket enthält `docs/windows-hardware-sensors.md` und `tools/windows/Set-PawnIoUserAccess.ps1` mit Installation, Verifikation und Rücknahme. Der signierte PawnIO-Installer wird separat vom Hersteller geladen. Installation und Berechtigungsänderungen benötigen einmalig Administratorrechte; die Anwendung ändert diese Rechte nicht automatisch. Die tatsächliche LIS2-Sensorquelle liefert nach der geprüften Freigabe auf dem i7-14700K CPU-Temperaturen ohne Administratorrechte. Ein Windows-Neustart und weitere CPU-/Mainboardmodelle bleiben als manuelle Prüfungen offen.

## Kompaktes Dashboard mit konfigurierbaren Livewerten

Die VFD-Karte nutzt die gesamte Breite mit unverändert kompakten 20×2 Zeichen links und Seitensteuerung rechts. Auswahl, vorherige/nächste Seite und Pause/Fortsetzen verwenden den aktiven Transport. Pause hält nur die automatische Rotation an; Werte, Laufschrift und Ereignisse bleiben aktiv. Vorschau und Bedienung passen auch bei Mindestbreite nebeneinander.

Acht frei beschriftbare Livewerte verwenden die vorhandene Vorlagensyntax und den Variablenbrowser. Das Zahnrad öffnet die Konfiguration; Speichern sichert die Auswahl im Workspace, Abbrechen verwirft den Entwurf. Die Lüfterkarte ergänzt konfigurierten Modus, Regelsensor und ausdrücklich zugeordnete Drehzahlsensoren. Konfigurierte Drehzahlquellen ohne Messwert zeigen „Nicht verfügbar“; es werden keine Drehzahlen aus Prozentwerten abgeleitet. Die Sensorzuordnung erteilt keine Hardwarefreigabe. Schnellzugriff und Demo-Schaltfläche entfallen. Die Hilfe beschreibt die Bedienung in allen fünf Sprachen.

## Flüssigere Winamp-Telemetrie

Das Winamp-Plug-in erfasst VU und Spektrum alle 100 ms. Jeder neue Snapshot weckt den Pipe-Worker sofort; die zusätzliche Wartezeit eines unabhängigen 200-ms-Workers entfällt. Der Metadaten-Cache bleibt bei einer Sekunde, mit sofortiger Erneuerung bei Quellen- oder Anzeigetitelwechsel. Die Anwendung übernimmt empfangene Windows-Medienwerte alle 100 ms. Winamp schließen, `gen_lis2.dll` durch die neue Release-Datei ersetzen und den Player wieder starten.

Der native Test prüft Erfassung und tatsächliche Pipe-Übertragung gemeinsam. Eine verlangsamte Erfassung dient als Gegenprobe; eine weitere Gegenprobe entfernt das Wecksignal des Workers. Der Oberflächentest prüft wechselnde Winamp-Stereopegel, alle zwanzig Spektrumbänder und moderne WMP-Metadaten gegen eine Zeitgrenze von 400 ms.

## Dashboard ohne redundanten Systemstatus

Der Systemstatusbereich und der Befehlszähler entfallen. Der Verbindungsstatus bleibt oben rechts sichtbar.

## Moderner Windows Media Player

Der GSMTC-Anbieter verarbeitet Änderungen von Titel, Wiedergabestatus und Zeitleiste ereignisgesteuert; die Oberfläche übernimmt empfangene Werte alle 100 ms. Ein neuer lokaler Paket-Smoke bestätigt Start, Verbindungserkennung, Shutdown und die Fehlermeldung bei fehlender Anbieter-DLL. Eine reale moderne WMP-Wiedergabe ist in diesem lokalen Prüflauf nicht aktiv. GSMTC liefert keine Audio-Samples; der moderne WMP hat weiterhin keine VU-/Spektrumsvariablen. Diese Prüfung ergänzt keine eigene Audioquelle für den modernen Player.

## Legacy-Audio ohne sichtbare Visualisierung

Pegel und Spektrum werden direkt aus dem Windows-Media-Player-Legacy-Prozess über WASAPI erfasst. Der Player darf minimiert sein; eine installierte oder ausgewählte LIS2-Visualisierung ist nicht erforderlich. Titel, Interpret und Laufzeit kommen weiterhin aus dem Hintergrund-Plug-in. Die bestehende Seitenausgabe mit `WmpLegacy.Vu` und `WmpLegacy.SpectrumGlyphs` bleibt nutzbar. Direkte Audioerfassung benötigt Windows-Build 20348 oder neuer, einschließlich Windows 11. Fehler werden angezeigt; es gibt keinen automatischen Wechsel zur unzuverlässigen Visualisierungsquelle.

Ein zehnminütiger realer Prototyplauf bestätigt Audio nach drei automatischen MP3-Titelwechseln sowie bei minimiertem Player. Die integrierte Medienkarte und die virtuelle VFD-Ausgabe sind mit realen Playerdaten geprüft. Die Hilfe beschreibt die direkte Audioquelle in fünf Sprachen. Weitere Zielsystem- und physische LIS2-Prüfungen bleiben separat offen.

## Feste VFD-Zeichenplätze

Dashboard und Seiteneditor zeichnen die Vorschau in zwanzig festen Zellen pro Zeile. Spektrumbalken und Unicode-Ersatzschriften verändern dadurch weder Breite noch Höhe. Die Größen-Gegenprobe reproduziert den Fehler mit den bisherigen Textblöcken. Gerenderte Prüfungen decken beide Ansichten bei 950×600 und 1305×893 ab; der Vorschautext bleibt für Bildschirmleser verfügbar.

## Flüssige Windows-Medienanzeige

Die Windows-Medienanzeige übernimmt bereits empfangene Telemetrie alle 100 ms. Pegel, Spektrum und die Medienwerte für Displayseiten erhalten dadurch den Takt der direkten Audioerfassung. Linux bleibt für externe playerctl-Abfragen bei einer Sekunde. Ein Oberflächentest verlangt mehrere aufeinanderfolgende Pegel- und Spektrumwechsel jeweils innerhalb von 400 ms.

## WMP-Legacy-Hintergrundverbindung

Das Hintergrund-Plug-in startet seine Telemetrie beim Empfang des Player-Kerns. Eine Plug-in-Oberfläche muss dafür nicht erzeugt werden. Abtrennen beendet den Timer, erneutes Verbinden startet ihn wieder. Der native Test liest tatsächliche Pipe-Snapshots ohne Oberflächenaufruf und prüft Lebenszyklus sowie erneutes Verbinden. Eine Gegenprobe entfernt den Hintergrundstart und muss am fehlenden Snapshot scheitern. Beide Architekturen erhalten die Korrektur.

Zum Aktualisieren den Legacy-Player schließen, die Hintergrund-DLL am registrierten Pfad durch die zur Playerarchitektur passende DLL ersetzen und den Player neu starten. Für den üblichen Player unter `Program Files (x86)` das win32-Paket verwenden. Für VU/Spektrum ist keine Visualisierung erforderlich.

## Streammetadaten für WMP und Linux

WMP Legacy liefert `WmpLegacy.StreamName` aus dem echten Feld `WM/RadioStationName`, die Wiedergabeadresse `StreamUrl` und das URL-basierte Kennzeichen `IsStream`. Fehlende Senderfelder bleiben leer; Titel und URLs werden nicht zu Sendernamen umgedeutet. Lokale Dateien leeren die Streamfelder. Beispiel: `{WmpLegacy.Artist|fallbackvar:WmpLegacy.StreamName|fallback:--}`. Die neuen Werte benötigen die neue Hintergrund-DLL passend zur Architektur des Players.

Der moderne Windows Media Player liefert zusätzlich `WindowsMedia.Subtitle`. Die GSMTC-Schnittstelle bietet keinen eigenen Sendernamen und keine Stream-URL. Ein Untertitel wird deshalb unverändert angeboten und kann ausdrücklich als Ersatz ausgewählt werden: `{WindowsMedia.Artist|fallbackvar:WindowsMedia.Subtitle|fallback:--}`. Artist und Title bleiben unverändert; ein Sendername ist damit nicht zugesichert.

Linux übernimmt Artist und Title des ausgewählten MPRIS-Players unverändert. Tuner kann bereits den Sendernamen als Artist liefern. Regressionen prüfen Streams, echte Interpretentags und fehlende Artist-Werte. Eine private D-Bus-Quelle wird mit echtem playerctl abgefragt, einschließlich Unicode und XML-Sonderzeichen. Ein Sendername wird nicht aus kombinierten Titeln geraten.

Die fünfsprachige Medienhilfe enthält Beispiele und Grenzen für alle drei Anbindungen. Beide Legacy-Architekturen erhalten einen nativen COM-Metadatentest. Die Legacy-Installation beziehungsweise Einbindung über die Player-GUI übernimmt der Benutzer; der moderne Medienanbieter wird mit der Anwendung aktualisiert.

## Ersatzvariablen und Winamp-Streamdaten

`{Winamp.Artist|fallbackvar:Winamp.StreamName|fallback:--}` verwendet zuerst Artist, bei fehlendem oder leerem Wert StreamName und zuletzt den festen Text `--`. Ebenso fehlen unknown und unavailable; Null, false und Leerzeichen bleiben Werte. Ersatzvariablen werden formatiert, fester Ersatztext bleibt unverändert. Es gibt keine rekursive Vorlagenauswertung. Der Formatierungseditor enthält Ersatzvariable mit eigenem Browser. Hilfe und Variablenbeschreibungen sind in allen fünf Sprachen ergänzt.

Die neue Winamp-DLL fragt UTF-16-Metadaten für Dateien und Streams ab. `Winamp.StreamName`, `StreamTitle`, `StreamGenre`, `StreamUrl` und `IsStream` stehen zusätzlich bereit. `TitleSource` nennt die Titelherkunft auch im Diagnosebericht. Lokale MP3s verwenden ihre Tags; Streams bevorzugen den aktuellen Streamtitel. Der gemeinsame Streamtitel wird nicht aufgeteilt und Artist nicht automatisch durch einen Sendernamen ersetzt. Die vom Decoder gelieferte StreamUrl kann von der Wiedergabeadresse abweichen.

Metadaten werden auf Winamps Fensterthread erfasst und zwischengespeichert; Pegel und Spektrum werden alle 200 ms, Metadaten spätestens nach einer Sekunde und bei erkanntem Quellen-/Anzeigetitelwechsel aktualisiert. Ein während der Abfrage erkannter Quellenwechsel verwirft den Snapshot. Quellenwechsel und Stop leeren alte Metadaten. Der Pipe-Thread überträgt jeden erfassten Snapshot höchstens einmal, damit ein hängender Player als veraltet erkennbar bleibt.

**Winamp 5.5 oder neuer:** Winamp schließen, `gen_lis2.dll` im Plugins-Verzeichnis ersetzen und den Player neu starten. Ältere DLLs liefern die neuen Felder nicht. Der native IPC-Test verwendet ein Testfenster; reale Wiedergabe im jeweiligen Winamp bleibt ein eigener Praxisnachweis.

## Leerzeichen in Präfix und Suffix

Der Vorlagenparser erhält führende und abschließende Leerzeichen in Präfix, Suffix und Ersatztext. Das gilt auch für Argumente, die nur aus Leerzeichen bestehen. Beispiel: `{Value|percent:0|prefix:Core 10: }` mit dem Wert `5` ergibt `Core 10: 5%`. Der Formatierungseditor zeigt die Abstände in seiner Vorschau und übernimmt sie beim Einfügen und Speichern. README und Hilfe enthalten Beispiele.

## Formatierungseditor und Hilfebeispiele

Der Seiteneditor enthält einen einklappbaren Formatierungseditor. Variablen lassen sich eingeben oder im vorhandenen Browser auswählen. Zahlenformat, 0 bis 6 Nachkommastellen, Groß-/Kleinschreibung, Präfix, Suffix und Ersatztext erzeugen einen Vorlagenausdruck mit Liveergebnis. Einfügen übernimmt ihn an die Schreibmarke in Zeile 1 oder 2; die freie Vorlageneingabe bleibt erhalten. Seite speichern sichert die eingefügten Ausdrücke. Die Hilfseditor-Einstellungen selbst werden nicht gespeichert.

Beide Zeilenfelder besitzen wieder Formatierungshinweise. Mauszeiger oder F1 im fokussierten Zeilenfeld öffnen den Hinweis; Escape schließt ihn. Die Seitenhilfe beschreibt den Editor und enthält ausführbare Beispiele für Zahlen, Prozent, Bytes, Präfix/Suffix, Groß-/Kleinschreibung und fehlende Werte. Alle neuen Texte sind in Deutsch, Englisch, Französisch, Türkisch und Russisch verfügbar.

Beispiel: `{Value|number:2}` formatiert `50.1234567` mit deutschem Betriebssystem-Zahlenformat als `50,12`, mit englischem als `50.12`. Die App-Sprache verändert das Zahlenformat des Betriebssystems nicht. Prozent ergänzt nur `%`, ohne mit 100 zu multiplizieren; Bytes verwendet 1024er-Schritte. Fehlende Werte geben den Ersatztext direkt zurück, ohne weitere Formatierung oder Suffix.

## Windows Media Player Legacy

Die Windows-App enthält die Legacy-Medienkarte, Metadaten über das Hintergrund-Plug-in und direkt erfasstes Prozess-Audio, `WmpLegacy.*`, drei Presets und fünfsprachige Hilfe. Die Pakete `LIS2-WMP-Legacy-0.1.17-win32.zip` und `LIS2-WMP-Legacy-0.1.17-win64.zip` enthalten jeweils beide nativen DLLs, Registrierungsanleitungen und die MIT-Lizenz. Die Architektur des Hintergrund-Plug-ins muss zum **Legacy-Playerprozess** passen. Registrierung mit dem passenden administrativen `regsvr32`, danach den Player neu starten. Die Visualisierungs-DLL bleibt ein optionales Diagnosewerkzeug. Einzelheiten und manuelle Prüfliste: [Legacy-Leitfaden](https://github.com/Eitschman/lis2-control-center/blob/v0.1.17/docs/wmp-legacy-integration.md). Kein Plug-in greift auf LIS2-Hardware zu.

Die Legacy-DLLs enthalten ihre C/C++-Laufzeit durch statisches Linken. Eine separate Visual-C++-Redistributable-Installation entfällt. Die CI kontrolliert die tatsächlichen DLL-Importe. Sicherheitsaktualisierungen der eingebundenen Laufzeit erfordern einen neuen Plug-in-Build.

## Gemeinsame Windows-/Linux-Anwendung

App, Protokolltester und Headless-Tests verwenden **Avalonia 11.3.22**. Die DBus-Sicherheitskorrektur bleibt eingebunden. About nennt Frameworkversion und MIT-Lizenz; eine lokalisierte Schaltfläche öffnet die Lizenzdateien. Die englische README enthält Installation, Lizenz und die Bibliotheksübersicht.

## Downloads und Installation

Enthalten sind portable Pakete für Windows x64, Linux x64 und Linux ARM64, Debian-Pakete für amd64/arm64, der Windows-Protokolltester, das native 32-Bit-Winamp-Plug-in `gen_lis2.dll` und beide Legacy-Plug-in-Pakete.

Unter Windows das ZIP entpacken und `LIS2.App.Avalonia.exe` starten. Unter Debian das passende Paket mit `sudo apt install ./lis2-control-center_0.1.23_amd64.deb` beziehungsweise `_arm64.deb` installieren. Portable Linux-Archive enthalten den Startbefehl `LIS2.App.Avalonia`. Prüfsummen stehen in `SHA256SUMS.txt`.

Die Anwendungspakete liefern **.NET 10.0.13** mit; .NET muss nicht separat installiert werden. Lizenztexte, Fremdkomponentenhinweise und feste Quellverweise liegen unter `licenses/` und `THIRD-PARTY-NOTICES.txt` bei. Avalonia-Lizenztext und Inter-Schrift wurden gegen den tatsächlichen Paketstand geprüft.

## Prüfumfang und offene Praxisprüfungen

[Main-CI 40911389643](https://github.com/Eitschman/lis2-control-center/actions/runs/40911389643) besteht zwölf Jobs, [Release-CI 40911390945](https://github.com/Eitschman/lis2-control-center/actions/runs/40911390945) alle 14. Linux besteht 658 Anwendungstests, Windows 657 mit einem übersprungenen Linux-Schlüsselbundtest. 32 Paketprüfungen bestehen. Alle neun heruntergeladenen Nutzdateien stimmen mit SHA256SUMS.txt überein. Produktversion 0.1.23, Runtime 10.0.13, Paketarchitekturen, Lizenzbeilagen, Debian-Empfehlungen und zehn README-Downloadlinks sind geprüft. Beide Legacy-Pakete bestehen Architektur- und statische Laufzeitprüfung; die Winamp-DLL besteht x86-PE- und Importprüfung. Der Windows-Paket-Smoke bestätigt Austausch und Wiederherstellung mit erneutem Desktopstart. Verbindungstests und Hilfe sind in fünf Sprachen gerendert; die Sichtprüfung umfasst unter anderem 1305×893 und 950×600 sowie helles und dunkles Theme. Die Suiten prüfen erfolgreiche, abgelehnte und ausbleibende HA-Anmeldung, unerwartete WebSocket-Begrüßung, fehlende Werkzeuge, veraltete Daten, Abbruch, Wiederholung, Shutdown und den Ausschluss vertraulicher Servermeldungen.

Die Release-Pipeline prüft Bibliotheken, Avalonia-Controls, native Windows-/Linux-Oberflächen, GSMTC-Anbieterstart, extrahierte Archive und Debian-Installation auf x64 und nativem ARM64. Die nativen Legacy-Plug-ins werden für beide Architekturen gebaut und mit Microsoft Defender geprüft. Die Desktop-Smokes prüfen unter anderem zehn Ansichten, Hilfe/About, Aktivierung aus einem zweiten Prozess und Shutdown. Screenshots dienen der Sichtprüfung.

Reale HA-/Playerverbindungen, die COM-Registrierung und Wiedergabe im tatsächlichen Legacy-Player, Anmeldung mit Autostart, weitere Desktopabläufe und physische LIS2-Ausgabe bleiben gesonderte Prüfungen. Pipe-Simulationen belegen keine reale Playerabnahme. Serielle Lüfterausgabe benötigt eine ausdrückliche Freigabe in der App.
