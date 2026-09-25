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
