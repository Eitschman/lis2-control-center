# gen_lis2.dll

Native 32-bit Winamp General Purpose Plugin for LIS2 Control Center.

Voraussetzung ist Winamp 5.5 oder neuer. Die DLL verwendet Unicode-IPC für Dateinamen, Titel und Metadaten. Die Schnittstellen sind im mitgelieferten ABI-Ausschnitt dokumentiert.

## Responsibilities

The plugin runs inside Winamp and:

- reads playback state;
- reads playlist position and length;
- reads elapsed time and duration;
- reads bitrate and sample rate;
- reads artist/title/album metadata;
- reads streamname, streamtitle, streamgenre and streamurl for streams, plus source type and title origin;
- sends normalized UTF-8 JSON snapshots to LIS2 Control Center.

It does **not**:

- access the LIS2 serial port;
- encode LIS2 commands;
- control fans;
- render pages.

## Transport

The plugin writes one JSON object per line to:

```text
\\.\pipe\LIS2ControlCenter.Winamp
```

If LIS2 Control Center is not running, the plugin simply keeps polling and retries the pipe on the next update.

## Build

The DLL must match classic Winamp's 32-bit process:

```powershell
cmake -S . -B build -A Win32
cmake --build build --config Release
```

Output:

```text
build\Release\gen_lis2.dll
```

Copy the DLL into Winamp's `Plugins` directory and restart Winamp.

Winamp vor dem Ersetzen schließen. Der aktuelle StreamTitle hat bei Streams Vorrang vor Titelmetadaten und Playlist-Anzeigetext. Lokale Dateien verwenden ihre Tags. Der Artist wird nicht automatisch ersetzt oder aus dem Streamtitel abgetrennt. Fehlende Streamfelder bleiben leer.

## Erfassung und Tests

Der 100-ms-Timer erfasst Snapshots im Winamp-Fensterthread. Jeder neue Snapshot weckt den Pipe-Worker sofort; ein zweiter Polling-Takt entfällt. Metadaten werden für bis zu eine Sekunde zwischengespeichert; ein Datei- oder Anzeigetitelwechsel erzwingt eine neue Abfrage. Stop leert den Cache. Datei, Anzeigetitel, Playlistposition und Wiedergabestatus werden vor dem Senden erneut geprüft. Ein abweichender Stand verwirft den Snapshot. Der Worker führt ausschließlich Pipe-I/O mit fertigen UTF-8-Snapshots aus. Die Anwendung übernimmt empfangene Windows-Medienwerte alle 100 ms.

`ctest --test-dir build -C Release --output-on-failure` prüft den echten Snapshotcode gegen ein IPC-Testfenster: Unicode, Datei-/Streamwechsel, Titelprioritäten, fehlende Tags, leere Felder, Metadatencache, Wechsel während einer Abfrage und Shutdown. Die GitHub-Windows-CI führt diesen Test vor dem Upload der DLL aus. Das ersetzt keine reale Winamp-Wiedergabe.

## ABI

Only the small subset of the Winamp ABI used by the plugin is declared in `winamp_sdk_min.h`. This keeps the project independent from proprietary installer content while making the interoperability surface explicit and reviewable.
