# LIS2 Windows Media Player Legacy plug-in

Native background UI plug-in for Windows Media Player Legacy.

It is telemetry-only. It never talks to LIS2 hardware.

`streamName` reads the actual `WM/RadioStationName` tag for network media. `streamUrl` contains the playback URL and `isStream` identifies non-file URLs. Missing station tags remain empty; local files clear both stream fields. Artist and title are preserved. Use `{WmpLegacy.Artist|fallbackvar:WmpLegacy.StreamName|fallback:--}` in the Control Center for an explicit station-name replacement. Older plug-ins do not provide these fields.

CTest exercises the COM metadata reader with Unicode radio names, missing tags, local files and unavailable media on both player architectures. These tests do not replace registration and playback checks in the actual player.

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

The MSVC build statically links the C/C++ runtime into both plug-in DLLs (`/MT`, `/MTd` for Debug). No separate Visual C++ Redistributable installation is required. CI checks the actual PE imports before packaging. Runtime security updates require rebuilding the plug-ins with an updated toolchain. The MIT license applies to LIS2 code; Microsoft runtime components remain under their [Microsoft redistribution terms](https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files).

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
