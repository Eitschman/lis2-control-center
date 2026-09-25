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

VU/spectrum is intentionally a separate follow-up component because Windows Media
Player only supplies timed waveform/frequency data to an active visualization plug-in.

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
