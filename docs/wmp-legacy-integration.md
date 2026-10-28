# Windows Media Player Legacy

Die Windows-App empfängt die Metadaten für `WmpLegacy.*` vom Hintergrund-Plug-in über eine Named Pipe. Dieses liefert Titel, Interpret, Album, Wiedergabestatus, Titelnummer, Playlistgröße, Position und Dauer. Stereo-VU und zwanzig Spektrumbänder werden über WASAPI direkt aus dem Legacy-Player-Prozess erfasst. Dafür muss keine Visualisierung installiert, ausgewählt oder sichtbar sein; der Player darf minimiert sein. Die direkte Audioerfassung benötigt Windows-Build 20348 oder neuer, etwa Windows 11. Der moderne Windows Media Player verwendet weiterhin GSMTC ohne Plug-in.

## Einrichtung

Beide DLLs binden die C/C++-Laufzeit statisch ein (`/MT` im Release-Build). Eine separate Visual-C++-Redistributable-Installation ist dafür nicht nötig. Die CI prüft die tatsächlichen PE-Importtabellen vor der Paketierung. Sicherheitsaktualisierungen dieser Laufzeit benötigen einen neuen Plug-in-Build mit aktualisierten Buildwerkzeugen. Die MIT-Lizenz gilt für den LIS2-Code; für eingebundene Microsoft-Laufzeitkomponenten gelten deren [Weitergabebedingungen](https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files).

1. Das zum **Playerprozess** passende CI-Artefakt `LIS2-WMP-Legacy-win32` oder `LIS2-WMP-Legacy-win64` laden und die SHA-256-Datei prüfen. Die Architektur der Control-Center-App ist dafür unerheblich. Release v0.1.16 enthält beide Architekturen als `LIS2-WMP-Legacy-0.1.16-win32.zip` und `LIS2-WMP-Legacy-0.1.16-win64.zip`.
2. Das ZIP in einen dauerhaften Ordner entpacken. `background` enthält `lis2_wmp_legacy.dll`, `visualization` enthält `lis2_wmp_legacy_visualization.dll`. Beide Ordner enthalten eigene Anleitungen.
3. Windows Media Player Legacy schließen. In einer administrativen Eingabeaufforderung die Hintergrund-DLL mit ihrem vollständigen, in Anführungszeichen gesetzten Pfad registrieren. Unter 64-Bit-Windows für einen x64-Player `%SystemRoot%\System32\regsvr32.exe` verwenden, für einen x86-Player `%SystemRoot%\SysWOW64\regsvr32.exe`. Beispiel für x64:

```bat
%SystemRoot%\System32\regsvr32.exe "C:\LIS2-Plugins\background\lis2_wmp_legacy.dll"
```

4. Control Center und Windows Media Player Legacy starten und einen Titel wiedergeben. Die Medienkarte zeigt Titel und Wiedergabestatus.
5. VU und Spektrum auf der Medienseite prüfen. Der Player kann anschließend minimiert werden. Die mitgelieferte Visualisierungs-DLL ist für die Anwendung nicht erforderlich und bleibt als separates Diagnosewerkzeug verfügbar.

Zum Entfernen den Player schließen und dieselben Registrierungsbefehle mit `/u` ausführen. Erst danach die DLL-Dateien verschieben oder löschen. Die Anwendung registriert keine DLL automatisch und benötigt für ihren normalen Betrieb keine Administratorrechte.

### Plug-in sichtbar, aber keine Verbindung

Zuerst die Architektur des laufenden Players prüfen: `C:\Program Files (x86)\Windows Media Player\wmplayer.exe` benötigt das win32-Paket und die Registrierung über `SysWOW64`. Die Architektur von Windows und Control Center bestimmt diese Auswahl nicht. Das Plug-in wird nach der Registrierung beim nächsten Playerstart geladen. Die Audioerfassung wartet auf eine aktive Metadatenverbindung und genau einen Legacy-Player in derselben Windows-Sitzung. Mehrere Player oder fehlende Betriebssystemunterstützung werden als Audiofehler angezeigt. Es erfolgt kein automatischer Wechsel auf den fehleranfälligen Visualisierungspfad.

Das Hintergrund-Plug-in startet seine Telemetrie bei der Übergabe des Player-Kerns über `SetCore`. Dafür ist keine Plug-in-Oberfläche erforderlich. Beim Abtrennen des Kerns endet der Timer; erneutes Verbinden startet ihn wieder. Der native Test empfängt tatsächliche Pipe-Snapshots, ohne `Create` aufzurufen, und prüft Abschalten sowie Wiederverbinden.

## Medienkarte und Displayseiten

### Audiowerte fehlen nach automatischem Titelwechsel

Auf dem geprüften Windows-11-System fällt die Audioübergabe bei automatischem Wechsel zwischen MP3-Titeln eines Albums aus. Titel und Laufzeit bleiben aktuell, Pegel und Spektrum stehen dagegen sowohl auf der Medienseite als auch in den Displayseiten still. Die Gegenprobe des Benutzers reproduziert denselben Ausfall mit einer eingebauten WMP-Visualisierung. Ein Wechsel zur Bibliotheks-/Wiedergabelistenansicht und zurück zur Visualisierungsansicht stellt Audio wieder her. Ein bloßer Visualisierungswechsel, Pause/Fortsetzen und ein Positionssprung haben es in dieser Prüfung nicht wiederhergestellt.

Der Visualisierungshersteller [SoundSpectrum dokumentiert den Ausfall nach dem ersten Titel](https://www.soundspectrum.com/g-force/Documentation/troubleshooting.html) als WMP-Fehler. Die Anwendung verwendet deshalb die direkte Prozess-Audioerfassung nach dem [Microsoft-Loopback-Vertrag](https://learn.microsoft.com/en-us/samples/microsoft/windows-classic-samples/applicationloopbackaudio-sample/). Sie erfasst nur den Playerprozess und dessen Unterprozesse. Aus PCM16-Stereo mit 44,1 kHz entstehen Spitzenpegel und zwanzig logarithmische Frequenzbänder im Bereich 40 Hz bis 20 kHz. Die Berechnung erfolgt ungefähr alle 100 ms. Ein zehnminütiger realer Prototyplauf liefert nach drei beobachteten Titelwechseln weiterhin Audiowerte und bestätigt Daten bei minimiertem Player. Wiedergabeposition und Playeransicht werden nicht automatisch verändert.

### Sendername als Ersatz für den Interpreten

`WmpLegacy.StreamName` liest das dokumentierte Attribut `WM/RadioStationName` des aktuellen Netzwerkmediums. Es bleibt leer, wenn der Player dieses Feld nicht liefert. `WmpLegacy.StreamUrl` enthält die Wiedergabeadresse; `WmpLegacy.IsStream` ist bei einer Nicht-Datei-URL wahr. Bei lokalen Dateien bleiben die Streamfelder leer und IsStream ist falsch. Ältere Hintergrund-DLLs liefern diese Felder nicht. Der Interpret wird nicht automatisch überschrieben, ein zusammengesetzter Titel wird nicht aufgeteilt.

```text
{WmpLegacy.Artist|fallbackvar:WmpLegacy.StreamName|fallback:--}
{WmpLegacy.Title|fallback:--}
```

Ein echter Interpret hat Vorrang. Fehlen Interpret und Senderfeld, erscheint `--`. Der Formatierungseditor kann StreamName über seinen Ersatzvariablenbrowser einfügen. Das setzt die neue Hintergrund-DLL passend zum Playerprozess voraus. Die Visualisierungs-DLL liefert weiterhin nur Audiotelemetrie.

Beim modernen Player steht stattdessen `WindowsMedia.Subtitle` für den unveränderten GSMTC-Untertitel zur Verfügung. Diese Schnittstelle hat kein eigenes Senderfeld und keine Stream-URL. `{WindowsMedia.Artist|fallbackvar:WindowsMedia.Subtitle|fallback:--}` ist eine ausdrückliche Untertitel-Auswahl, keine zugesicherte Sendererkennung.

Linux übernimmt `Media.Artist` und `Media.Title` aus MPRIS unverändert. Bei Tuner kann Artist bereits den Sendernamen enthalten. MPRIS definiert kein standardisiertes Senderfeld; LIS2 erfindet deshalb keines.

Die Karte zeigt Metadaten- und Audioverbindung getrennt an. Metadaten gelten nach drei Sekunden ohne Snapshot als getrennt, Audio nach zwei Sekunden. Abgelaufene Audiowerte werden geleert. Die bestehenden Variablennamen `VisualizationConnected` und `VisualizationAgeSeconds` bezeichnen weiterhin die Audioaktualität, jetzt aus direkter Erfassung. Playerende, erneuter Start und Ende der Metadatenverbindung beenden beziehungsweise erneuern die Erfassung derselben Quelle.

```text
{WmpLegacy.Artist}
{WmpLegacy.Title}
{WmpLegacy.State}
{WmpLegacy.TrackNumber} / {WmpLegacy.PlaylistCount}
{WmpLegacy.Elapsed} / {WmpLegacy.Duration}
{WmpLegacy.Vu}
{WmpLegacy.Spectrum}
{WmpLegacy.SpectrumGlyphs}
{WmpLegacy.Connected}
{WmpLegacy.VisualizationConnected}
```

Die Presets **WMP Legacy Now Playing**, **WMP Legacy VU** und **WMP Legacy Spectrum** sind unter Windows auswählbar. Ihre Sichtbarkeitsbedingung lautet `WmpLegacy.State=Playing`. Für `SpectrumGlyphs` muss das Spektrum-Glyphenset aktiv sein. Bei anderen Glyphensets erscheint das ASCII-Spektrum. Legacy-Pegel verwenden immer den Bereich 0 bis 255, auch bei leisen Signalen.

## Nachweise und reale Playerprüfung

Automatische Tests prüfen JSON-Abbildung, echte Metadaten-/Audiopipes, fehlerhafte JSON-Zeilen, Wiederverbinden, gleichzeitige Updates, getrennte Ablaufzeiten, Shutdown und erneuten Start. Avalonia-Tests prüfen Karte, Seitenerzeugung, Presets, Glyphen und Übersetzungen in fünf Sprachen. Build- und Paketnachweise stehen im [Status](prp/linux-port-status.md).

Eine simulierte Pipe-Verbindung belegt keine COM-Ladung durch den tatsächlichen Player. Für jede verwendete Playerarchitektur bleiben deshalb diese manuellen Prüfungen offen:

- Registrierung und Entfernen des Hintergrund-Plug-ins; Laden nach Playerneustart.
- Titelwechsel, Playlistgröße, Pause/Fortsetzen, Stop und Seek gegen die Medienkarte vergleichen.
- Ohne ausgewählte LIS2-Visualisierung Stereo-VU sowie zwanzig Spektrumbänder mit laufender Audiowiedergabe prüfen.
- Automatische Titelwechsel, Pause/Fortsetzen, Seek und minimierten Player prüfen. Andere Programme dürfen keine Audiowerte zum Player beitragen.
- Player beenden und neu starten, anschließend Control Center bei laufendem Player neu starten; Verbindungen müssen wiederhergestellt werden.
- Die drei Presets zunächst am virtuellen Display prüfen. Physische LIS2-Ausgabe bleibt ein eigener Gerätenachweis.

Playerpfad/-architektur, Windows-Version, CI-Commit und verwendetes Plug-in-Paket zusammen mit dem Ergebnis festhalten.
