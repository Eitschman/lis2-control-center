# Windows + Linux shared Avalonia port — implementation status

Date: 2026-10-13
Branch: `feature/linux-avalonia`
Status: **Shared Avalonia shell with virtual LIS2 session committed; latest changes awaiting CI validation**

## Binding UI acceptance criterion
- User confirmed 2026-10-13: The existing Windows WPF UI is the authoritative reference for **layout, placement, interactions, workflows and visual appearance** on BOTH Windows and Linux. The Avalonia sidebar/virtual buttons are temporary engineering scaffolding, not a proposed redesign.
- Before marking a screen done, compare against the WPF screen at matching resolution/DPI and document any unavoidable Avalonia differences; no arbitrary reorganization or missing controls.

## Target architecture
- ONE Avalonia UI and application runtime for Windows x64 and Linux x64/ARM64.
- Windows-only providers (Winamp, GSMTC, native plugins) and Linux-only providers (MPRIS, Linux sensors) are selected behind common interfaces.
- The WPF app remains an interim regression baseline, not a permanent separate Windows product.
- Both Windows and Linux builds, UI behavior, packaging and hardware safety must be validated before WPF retirement.

## Implemented
- Retargeted `LIS2.Core`, `LIS2.Display`, `LIS2.Fans` to `net8.0`, overriding the inherited Windows-only framework. Existing Windows WPF app remains unchanged.
- Retargeted their three xUnit test projects to `net8.0`.
- Added `.github/workflows/linux-portable.yml` to run these test projects on Ubuntu on branch pushes and PRs.

- Added `src/LIS2.App.Avalonia` with `net8.0`, Avalonia 11.3.6 references, `Program`, `App`, and a static main window displaying ten navigation labels. This is a nonfunctional UI shell, not a port of the existing WPF workflows.
- Expanded GitHub Actions matrix to Windows and Ubuntu for both portable unit tests and Avalonia shell compilation.

- Made all ten sidebar sections selectable in the shared Avalonia shell; selection updates the section heading and explicitly labels unavailable functionality. This is not feature parity.

- Confirmed GitHub Actions run 38406414651: all three portable test projects passed on Windows and Ubuntu; Avalonia builds failed with AVLN3000 due to SelectionChanged AXAML handler. Fixed by subscribing in code-behind (commits d85503f and 15756d6). User subsequently reported green CI; independently verifying the exact newer run remains outstanding.
- Added `VirtualDeviceSession` backed by the existing `VirtualLis2Transport` and `Lis2Device`; connect, disconnect and demo display commands can now be triggered from the shared Avalonia UI. No serial hardware or fan commands are issued. Latest code has not yet passed CI.

- Began WPF-faithful UI port by inspecting the actual `src/LIS2.App/MainWindow.xaml` baseline: restored 250px sidebar, minimum window dimensions, title/help/status header placement, transport/footer areas, dashboard 3:2 preview/connection cards, and virtual VFD text preview. This is a partial structural port only; no claim of full screen parity. Commits ee33220 and 8ca0dd7; new CI pending.

- Ported WPF dashboard layout sections (VFD Preview, System Status, Quick Links, four Fan Outputs in original two-column arrangement, tip panel) into Avalonia. Quick links navigate to matching sections; fan sliders and Apply remain intentionally disabled until safe fan runtime exists. This is UI structure only, not feature parity. Commits 7704688 and 74ddff7; CI pending.

- Display tab first-pass WPF layout port: Quick Display Test (two text lines, send, render both, clear), ROM/special character panel, CG Builder layout, brightness buttons. Quick display operations and brightness now call `Lis2Device` through the existing virtual transport, and dashboard preview reads actual `VirtualLis2State` instead of hard-coded text. ROM and CG Builder controls remain intentionally disabled pending functional port. No physical serial/fan control. Commits 4b3f6ef, 468b31e, 235e766, aff02d2; CI and desktop runtime validation pending.

- Ported original WPF special-character test and ROM 40-byte range navigation (`20`–`D8`) to virtual LIS2. Found and corrected a runtime-only protocol defect: `Lis2Protocol.WriteLine` requires lines **1/2**, whereas the Avalonia virtual demo/quick tests previously used **0/1**. Corrected both demo and quick-test calls; prior green CI only proved compilation, not functional behavior. Commits dcd24d6, 6b9bdb1, 6223ca1, 9ce5773, 6e656c1. CI and interactive tests pending.

- Added new `tests/LIS2.App.Avalonia.Tests` xUnit project with headless regression tests covering demo, 1-based display lines, brightness, clear, raw ROM ranges, invalid input, disconnected writes and special characters. Test project is committed but NOT yet wired into CI and not yet verified. Commits d773df5 and 4d4d92c.

- Added `tests/LIS2.App.Avalonia.Tests` xUnit project and CI job on both Windows and Linux to execute headless virtual-session regression tests (display line indexing, ROM ranges, brightness, clear, connection guard, special characters). Expanded tests for 5x8 glyph validation and eight-row programming. The CG Builder now has editable slot selection and binary row input with a working virtual send operation; saved glyph sets, graphical pixel editor and persistence are still pending. Relevant commits d773df5 through 6996bee. **New CI jobs are not yet confirmed green at this documentation commit.**

- CG Builder functional port expanded: original WPF `CustomGlyphSettings`, `CustomGlyphSetFile` and `BuiltInGlyphSets` copied to the cross-platform Avalonia project (same versioned `.lis2glyphs.json` schema); `GlyphWorkspace` implements eight slots, name/bitmap editing, validated JSON import/export, original examples; Avalonia storage-provider dialogs load/save sets, live 5x8 pixel preview, send-all 64-row virtual protocol operation, and optional virtual-connect auto-programming. Added workspace roundtrip/examples/send-all tests. Note: Save glyph updates in-memory workspace only; persistent app settings, per-pixel mouse grid, named template binding and real-device integration remain pending. Main integrated commit a31f630. CI on latest head pending.

## Verification and limitations
- Static scan of the three library trees found no direct WPF, WinForms, Registry, WinRT, or P/Invoke usage.
- The `System.IO.Ports` dependency remains and actual serial device behavior on Linux is **not** verified.
- No .NET SDK is installed in the current execution environment; local `dotnet build` / `dotnet test` could not be run.
- Run 38406414651 was inspected including job logs; earlier AVLN3000 failure is fixed. New virtual UI integration still requires CI validation and real desktop smoke testing.
- User reported the previous CI run green; newly changed WPF-aligned UI must still pass its own Windows/Linux build and GUI startup checks.
- The Windows release workflow and WPF source files have not been modified.

## Next work
1. Confirm Ubuntu workflow success and Windows solution CI.
2. Introduce platform-neutral application runtime with tests while preserving WPF behavior.
3. Confirm the virtual-session Avalonia shell builds and launches on Windows and Linux; add automated virtual-device tests and shared view models.
4. Implement Windows and Linux providers, then UI feature parity and physical-device safety validation on both systems.

## Pull request gate
Open a PR against `main` only when a coherent, tested slice is ready. Keep this branch isolated until then.
