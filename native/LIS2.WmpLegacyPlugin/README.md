# LIS2 Windows Media Player Legacy plug-in

Native background UI plug-in for Windows Media Player Legacy.

It is telemetry-only. It never talks to LIS2 hardware.

## Telemetry

The plug-in publishes JSON snapshots to:

```text
\\.\pipe\LIS2ControlCenter.WmpLegacy
```

Current fields:

- playback state
- artist
- title
- album
- track number
- playlist count
- elapsed time
- duration

VU/spectrum comes from the companion `lis2_wmp_legacy_visualization.dll`, included
in the same package. Windows Media Player only supplies timed waveform/frequency
data to the selected visualization. Metadata works independently of that selection.

## Registration

The DLL is an in-process COM server and must match the bitness of the target
Windows Media Player process.

Run an elevated command prompt:

```text
regsvr32 lis2_wmp_legacy.dll
```

To remove it:

```text
regsvr32 /u lis2_wmp_legacy.dll
```

The registration installs a hidden, auto-run background WMP UI plug-in.

Both x86 and x64 builds are produced by CI; install the one matching the WMP Legacy
process that is actually used.

On 64-bit Windows, use `%SystemRoot%\System32\regsvr32.exe` for an x64
player and `%SystemRoot%\SysWOW64\regsvr32.exe` for an x86 player. Use the
full quoted path to the DLL. Keep the DLL in its registered location; unregister
it before moving or deleting it. Registration requires administrator rights.
The x64 Control Center can receive telemetry from either player architecture.
