# Windows Media Player Legacy

Die Windows-App empfängt `WmpLegacy.*` über zwei voneinander unabhängige Named Pipes. Das Hintergrund-Plug-in liefert Titel, Interpret, Album, Wiedergabestatus, Titelnummer, Playlistgröße, Position und Dauer. Die optionale Visualisierung liefert Stereo-VU und zwanzig Spektrumbänder. Beide Plug-ins senden ausschließlich Telemetrie und greifen nicht auf LIS2-Hardware zu. Der moderne Windows Media Player verwendet weiterhin GSMTC ohne Plug-in.

## Einrichtung

Beide DLLs binden die C/C++-Laufzeit statisch ein (`/MT` im Release-Build). Eine separate Visual-C++-Redistributable-Installation ist dafür nicht nötig. Die CI prüft die tatsächlichen PE-Importtabellen vor der Paketierung. Sicherheitsaktualisierungen dieser Laufzeit benötigen einen neuen Plug-in-Build mit aktualisierten Buildwerkzeugen. Die MIT-Lizenz gilt für den LIS2-Code; für eingebundene Microsoft-Laufzeitkomponenten gelten deren [Weitergabebedingungen](https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files).

1. Das zum **Playerprozess** passende CI-Artefakt `LIS2-WMP-Legacy-win32` oder `LIS2-WMP-Legacy-win64` laden und die SHA-256-Datei prüfen. Die Architektur der Control-Center-App ist dafür unerheblich. Release v0.1.9 enthält beide Architekturen als `LIS2-WMP-Legacy-0.1.9-win32.zip` und `LIS2-WMP-Legacy-0.1.9-win64.zip`.
2. Das ZIP in einen dauerhaften Ordner entpacken. `background` enthält `lis2_wmp_legacy.dll`, `visualization` enthält `lis2_wmp_legacy_visualization.dll`. Beide Ordner enthalten eigene Anleitungen.
3. Windows Media Player Legacy schließen. In einer administrativen Eingabeaufforderung beide DLLs mit ihrem vollständigen, in Anführungszeichen gesetzten Pfad registrieren. Unter 64-Bit-Windows für einen x64-Player `%SystemRoot%\System32\regsvr32.exe` verwenden, für einen x86-Player `%SystemRoot%\SysWOW64\regsvr32.exe`. Beispiel für x64:

```bat
%SystemRoot%\System32\regsvr32.exe "C:\LIS2-Plugins\background\lis2_wmp_legacy.dll"
%SystemRoot%\System32\regsvr32.exe "C:\LIS2-Plugins\visualization\lis2_wmp_legacy_visualization.dll"
```

4. Control Center und Windows Media Player Legacy starten und einen Titel wiedergeben. Die Medienkarte zeigt Titel und Wiedergabestatus.
5. Für VU und Spektrum die Visualisierung **LIS2 Control Center / Telemetry** im Player auswählen. Andere Visualisierungen liefern diesem Plug-in kein Audiosignal. Metadaten funktionieren unabhängig davon.

Zum Entfernen den Player schließen und dieselben Registrierungsbefehle mit `/u` ausführen. Erst danach die DLL-Dateien verschieben oder löschen. Die Anwendung registriert keine DLL automatisch und benötigt für ihren normalen Betrieb keine Administratorrechte.

## Medienkarte und Displayseiten

### Sendername als Ersatz für den Interpreten

`WmpLegacy.StreamName` liest das dokumentierte Attribut `WM/RadioStationName` des aktuellen Netzwerkmediums. Es bleibt leer, wenn der Player dieses Feld nicht liefert. `WmpLegacy.StreamUrl` enthält die Wiedergabeadresse; `WmpLegacy.IsStream` ist bei einer Nicht-Datei-URL wahr. Bei lokalen Dateien bleiben die Streamfelder leer und IsStream ist falsch. Ältere Hintergrund-DLLs liefern diese Felder nicht. Der Interpret wird nicht automatisch überschrieben, ein zusammengesetzter Titel wird nicht aufgeteilt.

```text
{WmpLegacy.Artist|fallbackvar:WmpLegacy.StreamName|fallback:--}
{WmpLegacy.Title|fallback:--}
```

Ein echter Interpret hat Vorrang. Fehlen Interpret und Senderfeld, erscheint `--`. Der Formatierungseditor kann StreamName über seinen Ersatzvariablenbrowser einfügen. Das setzt die neue Hintergrund-DLL passend zum Playerprozess voraus. Die Visualisierungs-DLL liefert weiterhin nur Audiotelemetrie.

Beim modernen Player steht stattdessen `WindowsMedia.Subtitle` für den unveränderten GSMTC-Untertitel zur Verfügung. Diese Schnittstelle hat kein eigenes Senderfeld und keine Stream-URL. `{WindowsMedia.Artist|fallbackvar:WindowsMedia.Subtitle|fallback:--}` ist eine ausdrückliche Untertitel-Auswahl, keine zugesicherte Sendererkennung.

Linux übernimmt `Media.Artist` und `Media.Title` aus MPRIS unverändert. Bei Tuner kann Artist bereits den Sendernamen enthalten. MPRIS definiert kein standardisiertes Senderfeld; LIS2 erfindet deshalb keines.

Die Karte zeigt Metadatenverbindung und Visualisierungsverbindung getrennt an. Metadaten gelten nach drei Sekunden ohne Snapshot als getrennt, Audio nach zwei Sekunden. Veraltete Titel verschwinden aus Karte und Seitenwerten; abgelaufene Audiowerte werden geleert. Die Listener nehmen nach dem Ende einer Playersitzung neue Verbindungen an.

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

- Registrierung und Entfernen beider Plug-ins; Laden des Hintergrund-Plug-ins nach Playerneustart.
- Titelwechsel, Playlistgröße, Pause/Fortsetzen, Stop und Seek gegen die Medienkarte vergleichen.
- Die LIS2-Visualisierung auswählen und Stereo-VU sowie zwanzig Spektrumbänder mit laufender Audiowiedergabe prüfen.
- Visualisierung wechseln: Titel bleiben aktuell, Audio wird nach der Frist geleert; Rückwechsel stellt Audio wieder her.
- Player beenden und neu starten, anschließend Control Center bei laufendem Player neu starten; Verbindungen müssen wiederhergestellt werden.
- Die drei Presets zunächst am virtuellen Display prüfen. Physische LIS2-Ausgabe bleibt ein eigener Gerätenachweis.

Playerpfad/-architektur, Windows-Version, CI-Commit und verwendetes Plug-in-Paket zusammen mit dem Ergebnis festhalten.
