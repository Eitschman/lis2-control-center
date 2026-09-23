# gen_lis2.dll

Native 32-bit Winamp General Purpose Plugin for LIS2 Control Center.

## Responsibilities

The plugin runs inside Winamp and:

- reads playback state;
- reads playlist position and length;
- reads elapsed time and duration;
- reads bitrate and sample rate;
- reads artist/title/album metadata;
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

## ABI

Only the small subset of the Winamp ABI used by the plugin is declared in `winamp_sdk_min.h`. This keeps the project independent from proprietary installer content while making the interoperability surface explicit and reviewable.
