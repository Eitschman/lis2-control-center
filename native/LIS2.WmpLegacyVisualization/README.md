# LIS2 WMP Legacy visualization

Optional Windows Media Player Legacy visualization that forwards audio telemetry
to LIS2 Control Center.

The WMP visualization API supplies a `TimedLevel` snapshot containing 1024
frequency bins and 1024 waveform samples for each stereo channel. The plug-in
reduces the frequency data to 20 bands and derives left/right VU peak values.

Telemetry pipe:

```text
\\.\pipe\LIS2ControlCenter.WmpLegacy.Visualization
```

The visualization is telemetry-only and never talks to LIS2 hardware.

## Installation

The MSVC build statically links the C/C++ runtime into both plug-in DLLs (`/MT`, `/MTd` for Debug). No separate Visual C++ Redistributable installation is required. CI checks the actual PE imports before packaging. Runtime security updates require rebuilding the plug-ins with an updated toolchain. The MIT license applies to LIS2 code; Microsoft runtime components remain under their [Microsoft redistribution terms](https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files).

Register the DLL that matches the bitness of Windows Media Player Legacy from an
elevated command prompt:

```text
regsvr32 lis2_wmp_legacy_visualization.dll
```

Then select **LIS2 Control Center / Telemetry** as the active visualization in
Windows Media Player Legacy. WMP supplies visualization audio data only while
the visualization is loaded.

Uninstall with:

```text
regsvr32 /u lis2_wmp_legacy_visualization.dll
```

On 64-bit Windows, use `%SystemRoot%\System32\regsvr32.exe` for an x64
player and `%SystemRoot%\SysWOW64\regsvr32.exe` for an x86 player. Use the
full quoted path to the DLL. Keep the DLL in its registered location; unregister
it before moving or deleting it. Registration requires administrator rights.
The x64 Control Center can receive telemetry from either player architecture.
