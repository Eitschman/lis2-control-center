# Winamp Integration

Winamp is a first-class LIS2 Control Center data source.

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

The planned `gen_lis2.dll` will be a thin Winamp general-purpose plugin.

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
