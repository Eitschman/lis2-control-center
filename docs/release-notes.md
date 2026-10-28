# LIS2 Control Center 0.1.14

## Kompaktes Dashboard mit konfigurierbaren Livewerten

Die VFD-Karte nutzt die gesamte Breite mit unverändert kompakten 20×2 Zeichen links und Seitensteuerung rechts. Auswahl, vorherige/nächste Seite und Pause/Fortsetzen verwenden den aktiven Transport. Pause hält nur die automatische Rotation an; Werte, Laufschrift und Ereignisse bleiben aktiv. Vorschau und Bedienung passen auch bei Mindestbreite nebeneinander.

Vier frei beschriftbare Livewerte verwenden die vorhandene Vorlagensyntax und den Variablenbrowser. Das Zahnrad öffnet die Konfiguration; Speichern sichert die Auswahl im Workspace, Abbrechen verwirft den Entwurf. Die Lüfterkarte ergänzt konfigurierten Modus, Regelsensor und ausdrücklich zugeordnete Drehzahlsensoren. Ohne passende Drehzahlquelle erscheint „Nicht verfügbar“; es werden keine Drehzahlen aus Prozentwerten abgeleitet. Die Sensorzuordnung erteilt keine Hardwarefreigabe. Schnellzugriff und Demo-Schaltfläche entfallen. Die Hilfe beschreibt die Bedienung in allen fünf Sprachen.

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

Die Windows-App enthält die Legacy-Medienkarte, Metadaten über das Hintergrund-Plug-in und direkt erfasstes Prozess-Audio, `WmpLegacy.*`, drei Presets und fünfsprachige Hilfe. Die Pakete `LIS2-WMP-Legacy-0.1.14-win32.zip` und `LIS2-WMP-Legacy-0.1.14-win64.zip` enthalten jeweils beide nativen DLLs, Registrierungsanleitungen und die MIT-Lizenz. Die Architektur des Hintergrund-Plug-ins muss zum **Legacy-Playerprozess** passen. Registrierung mit dem passenden administrativen `regsvr32`, danach den Player neu starten. Die Visualisierungs-DLL bleibt ein optionales Diagnosewerkzeug. Einzelheiten und manuelle Prüfliste: [Legacy-Leitfaden](https://github.com/Eitschman/lis2-control-center/blob/v0.1.14/docs/wmp-legacy-integration.md). Kein Plug-in greift auf LIS2-Hardware zu.

Die Legacy-DLLs enthalten ihre C/C++-Laufzeit durch statisches Linken. Eine separate Visual-C++-Redistributable-Installation entfällt. Die CI kontrolliert die tatsächlichen DLL-Importe. Sicherheitsaktualisierungen der eingebundenen Laufzeit erfordern einen neuen Plug-in-Build.

## Gemeinsame Windows-/Linux-Anwendung

App, Protokolltester und Headless-Tests verwenden **Avalonia 11.3.22**. Die DBus-Sicherheitskorrektur bleibt eingebunden. About nennt Frameworkversion und MIT-Lizenz; eine lokalisierte Schaltfläche öffnet die Lizenzdateien. Die englische README enthält Installation, Lizenz und die Bibliotheksübersicht.

## Downloads und Installation

Enthalten sind portable Pakete für Windows x64, Linux x64 und Linux ARM64, Debian-Pakete für amd64/arm64, der Windows-Protokolltester, das native 32-Bit-Winamp-Plug-in `gen_lis2.dll` und beide Legacy-Plug-in-Pakete.

Unter Windows das ZIP entpacken und `LIS2.App.Avalonia.exe` starten. Unter Debian das passende Paket mit `sudo apt install ./lis2-control-center_0.1.14_amd64.deb` beziehungsweise `_arm64.deb` installieren. Portable Linux-Archive enthalten den Startbefehl `LIS2.App.Avalonia`. Prüfsummen stehen in `SHA256SUMS.txt`.

Die Anwendungspakete liefern **.NET 10.0.13** mit; .NET muss nicht separat installiert werden. Lizenztexte, Fremdkomponentenhinweise und feste Quellverweise liegen unter `licenses/` und `THIRD-PARTY-NOTICES.txt` bei. Avalonia-Lizenztext und Inter-Schrift wurden gegen den tatsächlichen Paketstand geprüft.

## Prüfumfang und offene Praxisprüfungen

Die Release-Pipeline prüft Bibliotheken, Avalonia-Controls, native Windows-/Linux-Oberflächen, GSMTC-Anbieterstart, extrahierte Archive und Debian-Installation auf x64 und nativem ARM64. Die nativen Legacy-Plug-ins werden für beide Architekturen gebaut und mit Microsoft Defender geprüft. Die Desktop-Smokes prüfen unter anderem zehn Ansichten, Hilfe/About, Aktivierung aus einem zweiten Prozess und Shutdown. Screenshots dienen der Sichtprüfung.

Reale HA-/Playerverbindungen, die COM-Registrierung und Wiedergabe im tatsächlichen Legacy-Player, Anmeldung mit Autostart, weitere Desktopabläufe und physische LIS2-Ausgabe bleiben gesonderte Prüfungen. Pipe-Simulationen belegen keine reale Playerabnahme. Serielle Lüfterausgabe benötigt eine ausdrückliche Freigabe in der App.
