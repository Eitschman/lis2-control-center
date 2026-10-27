# Winamp Integration

Winamp is a first-class LIS2 Control Center data source.

## Plattformumfang des Avalonia-Branches

Stand 20.10.2026, PORT-01: Die gemeinsame App startet unter Windows den vorhandenen Winamp-Named-Pipe-Anbieter. `Winamp.*` bleibt erhalten. GSMTC wird separat aus `LIS2.WindowsMedia.dll` geladen und liefert `WindowsMedia.*`; die Windows-Paketierung kopiert Provider und DLL-Abhängigkeiten. Reale Player-/Paketstarts sind durch Headless-Tests und Paketdateiprüfung nicht belegt; aktuelle Nachweise stehen im [Status](prp/linux-port-status.md).

Linux fragt über `playerctl metadata --format` periodisch Status, Titel, Interpret, Album und Dauer ab. Die Werte erscheinen unter `Media.*`. Eine eigene D-Bus-Subscription, Zeitposition und Playerauswahl sind nicht implementiert. Linux-Metadaten liefern kein Winamp-VU-/Spektrumsignal. VLC/Plexamp und Wine/Winamp sind nicht als aktuelle Laufzeitnachweise dokumentiert.

Die Seitenerzeugung aus allen Medienkarten verwendet `PageWorkspaceActions`: aktuellen Entwurf validieren und übernehmen, neue Seite auswählen, laufende Sequenzen aktualisieren und speichern. Ungültige Entwürfe verhindern den Wechsel.

Die Avalonia-App startet unter Windows zusätzlich `WmpLegacyDataSource`. Eine eigene Medienkarte, `WmpLegacy.*` und drei Presets verwenden getrennte Metadaten- und Audiopipes. Der gemeinsame Workflow baut die zwei nativen Legacy-Plug-ins für x86/x64. Einrichtung und offene reale Playerprüfung stehen im [Legacy-Leitfaden](wmp-legacy-integration.md).

## Architecture

```text
Winamp
  |
  v
gen_lis2.dll
  |
  | JSON lines over Windows Named Pipe
  v
LIS2.Winamp / WinampPipeServer
  |
  v
WinampDataSource
  |
  v
Display engine / event engine
```

Named pipe:

```text
\\.\pipe\LIS2ControlCenter.Winamp
```

The plugin does not write to LIS2 hardware. It only gathers Winamp metadata/events and sends normalized messages to the host application.

## Herkunft von Interpret und Titel

`native/LIS2.WinampPlugin/plugin.cpp` liest die tatsächlich gespielte Datei beziehungsweise Stream-URL über `IPC_GET_PLAYING_FILENAME` (3031) und den Anzeigetitel über `IPC_GET_PLAYING_TITLE` (3034). Winamp 5.5 oder neuer ist erforderlich. `GetExtendedInfo` fragt UTF-16-Werte über `IPC_GET_EXTENDED_FILE_INFOW_HOOKABLE` (3027) und bei nicht unterstütztem Hook über `IPC_GET_EXTENDED_FILE_INFOW` (3026) ab. Lokale MP3s verwenden `artist`, `title` und `album` aus den vom Decoder angebotenen Tags.

Streams liefern zusätzlich `Winamp.StreamName`, `StreamTitle`, `StreamGenre`, `StreamUrl` und `IsStream`. Title bevorzugt den aktuellen StreamTitle, dann Titelmetadaten, zuletzt den Playlist-Anzeigetext. Artist bleibt ein echtes Metadatenfeld; der gemeinsame Streamtitel wird nicht aufgeteilt. `Winamp.TitleSource` nennt die Herkunft als `FileTags`, `StreamMetadata`, `StreamTitle`, `Playlist` oder `None`, auch im Diagnosebericht. StreamUrl ist die vom Decoder gelieferte URL und kann von der Wiedergabeadresse abweichen. Fehlende Felder bleiben leer; ältere DLLs liefern die neuen Felder nicht.

Metadaten und Audiotelemetrie werden auf Winamps Fensterthread erfasst. Ein Cache begrenzt Metadatenabfragen auf eine Sekunde, außer bei Datei-/Anzeigetitelwechsel. Audiotelemetrie und Snapshots werden alle 100 ms erfasst. Neue Snapshots wecken den Pipe-Worker sofort, ohne einen zweiten Polling-Takt. Stop und Quellenwechsel ersetzen alte Felder. Ändern sich Quelle, Anzeigetitel, Playlistposition oder Wiedergabestatus während der Erfassung, wird der Snapshot verworfen. Der Pipe-Thread überträgt nur fertige UTF-8-Nachrichten. Der native CTest prüft den Snapshotcode mit einem Winamp-IPC-Testfenster und die Übertragungsrate über eine echte Pipe; echte Wiedergabe bleibt ein eigener Praxisnachweis.

Für Artist mit Sendername als Ersatz kann eine Seite `{Winamp.Artist|fallbackvar:Winamp.StreamName|fallback:--}` verwenden. Fehlende, leere, unknown und unavailable Werte wechseln zur Ersatzvariable; Null, false und Leerzeichen bleiben gültige Werte. Ersatzvariablen werden normal formatiert, der feste Ersatztext bleibt unverändert. Die Auswertung ist nicht rekursiv. Im Formatierungseditor gibt es dafür Ersatzvariable mit eigenem Browser.

Die [Winamp-Implementierung](https://github.com/alexfreud/winamp/blob/community/Src/Plugins/Input/in_mp3/ExtendedInfo.cpp) bietet die Streamfelder an. Der direkte ICY-Abruf von `http://streams.radiobob.de/symphmetal/mp3-192/mediaplayer/` am 27.10.2026 mit `Icy-MetaData: 1` liefert zunächst `StreamTitle='RADIO BOB - Symphonic Metal';`, dann `StreamTitle='Lacrimosa - Wenn unsere Helden sterben';`. In 120 gelesenen Metadatenintervallen über 21,1 Sekunden werden keine getrennten Artist-/Title-Felder geliefert. Tuner verwendet laut [MPRIS-Implementierung](https://github.com/tuner-labs/tuner/blob/v2.1.0/src/Services/DBusMediaPlayer.vala#L208-L215) bei fehlendem Artist den Sendernamen.

## Geprüfter Streambetrieb

Am 27.10.2026 liefert der laufende Winamp 5.9.2.10042 für `http://streams.radiobob.de/powermetal/mp3-192/mediaplayer/` über die tatsächlichen IPC-Abfragen leere Werte für artist, title und album, `RADIO BOB - Power Metal` für streamname und `Powerwolf - Blessed & Possessed` für streamtitle. streamgenre und streamurl sind bei diesem Stream leer. Der Benutzer bestätigt die Anzeige mit `{Winamp.Artist|fallbackvar:Winamp.StreamName|fallback:--}` in LIS2 v0.1.8 nach Installation der geprüften Release-DLL. Ihre SHA-256-Prüfsumme stimmt mit dem veröffentlichten Paket überein. Lokale MP3-Wiedergabe ist durch diesen Praxisnachweis nicht abgedeckt.

Winamp muss beim Austausch geschlossen sein. Maßgeblich ist das Plugins-Verzeichnis der tatsächlich gestarteten Installation; der installierte Dateihash muss zur Release-Prüfsumme passen.

## Message format

Messages are UTF-8 JSON, one object per line.

Example:

```json
{
  "type": "snapshot",
  "state": "playing",
  "artist": "Iron Maiden",
  "title": "The Trooper",
  "album": "Piece of Mind",
  "playlistPosition": 3,
  "playlistCount": 12,
  "elapsedSeconds": 65,
  "durationSeconds": 245,
  "bitrateKbps": 320,
  "sampleRateHz": 44100
}
```

Supported playback states:

- `playing`
- `paused`
- `stopped`

Unknown/missing values are allowed.

## Display variables

The host exposes values with the `Winamp.` prefix:

- `{Winamp.State}`
- `{Winamp.Artist}`
- `{Winamp.Title}`
- `{Winamp.Album}`
- `{Winamp.PlaylistPosition}`
- `{Winamp.PlaylistCount}`
- `{Winamp.Elapsed}`
- `{Winamp.Duration}`
- `{Winamp.BitrateKbps}`
- `{Winamp.SampleRateHz}`

## Development without Winamp

`tools/LIS2.WinampSimulator` connects to the same pipe and sends realistic playback snapshots.

Run the main application first, then:

```powershell
dotnet run --project .\tools\LIS2.WinampSimulator\
```

This validates the host integration, data source and display templates before the native Winamp plugin exists.

## Native plugin

The native x86 `gen_lis2.dll` is implemented as a thin Winamp general-purpose plug-in and is built by CI.

Responsibilities:

- detect playback state
- detect track changes
- collect metadata
- collect playlist position/count
- collect elapsed/duration
- send normalized snapshots/events through the named pipe
- reconnect when LIS2 Control Center restarts

Non-responsibilities:

- no serial-port access
- no LIS2 protocol encoding
- no fan control
- no page rendering

This separation keeps the native plugin intentionally small and makes the host side independently testable.


## VU and spectrum telemetry

The native general-purpose plug-in also retrieves Winamp's in-process visualization data.

- `IPC_GETVUDATAFUNC` supplies per-channel VU values in the range 0..255.
- `IPC_GETSADATAFUNC` parameter 2 supplies the Winamp 5.5+ analyzer callback; the plug-in passes its own 158-byte buffer and receives 150 bytes of analyzer data. Parameter 0 is the deprecated static-buffer API and is intentionally not used.
- `gen_lis2.dll` downsamples the analyzer data to 20 bands before sending it over the named pipe.
- Audio-Snapshots werden mit 10 Hz (100 ms) erfasst und ohne zusätzlichen Worker-Polling-Takt gesendet. Die Windows-Medienanzeige übernimmt empfangene Daten ebenfalls alle 100 ms, wie bei WMP Legacy. Die Displayausgabe verwendet weiterhin differenzielle Schreibvorgänge.

The named-pipe message can additionally contain:

```json
{
  "vuLeft": 180,
  "vuRight": 164,
  "spectrum": [12, 18, 24, 40, 88, 120, 90, 64, 48, 32, 20, 16, 12, 8, 6, 4, 3, 2, 1, 0]
}
```

The application exposes these as:

- `{Winamp.VuLeft}`
- `{Winamp.VuRight}`
- `{Winamp.Vu}` — a preformatted 20-character stereo VU line
- `{Winamp.Spectrum}` — a preformatted 20-character spectrum line
- `{Winamp.SpectrumGlyphs}` — 20-cell spectrum rendered with eight programmable VFD glyph levels

Visualization data is optional. Older plug-ins / simulators that omit these fields remain compatible.

The application provides ready-made **Now Playing**, **VU** and **Spectrum** page presets. The Spectrum preset can install a project-owned eight-glyph bar set into all custom-character slots after explicit user confirmation.

The application treats Winamp as disconnected after snapshots become stale. This deliberately forces `Winamp.State` back to `Unknown` for page visibility evaluation so a `Winamp.State=Playing` page cannot remain stuck after Winamp or the plug-in exits.
