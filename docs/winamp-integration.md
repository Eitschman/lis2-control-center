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
- visualization snapshots are currently sent at a deliberately modest 5 Hz (200 ms) cadence so the 20x2 VFD and serial link are not treated like a high-frame-rate visualization device.

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
