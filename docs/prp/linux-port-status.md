# Linux/Avalonia port — implementation status

Date: 2026-10-13
Branch: `feature/linux-avalonia`
Status: **Phase 1 groundwork only — no Linux desktop app yet**

## Implemented
- Retargeted `LIS2.Core`, `LIS2.Display`, `LIS2.Fans` to `net8.0`, overriding the inherited Windows-only framework. Existing Windows WPF app remains unchanged.
- Retargeted their three xUnit test projects to `net8.0`.
- Added `.github/workflows/linux-portable.yml` to run these test projects on Ubuntu on branch pushes and PRs.

## Verification and limitations
- Static scan of the three library trees found no direct WPF, WinForms, Registry, WinRT, or P/Invoke usage.
- The `System.IO.Ports` dependency remains and actual serial device behavior on Linux is **not** verified.
- No .NET SDK is installed in the current execution environment; local `dotnet build` / `dotnet test` could not be run.
- The GitHub Actions workflow has been committed, but its result has not been confirmed. Do not mark Linux tests green until Actions succeeds.
- No Avalonia packages or GUI have been added yet; no claims of an operational Linux build.
- The Windows release workflow and WPF source files have not been modified.

## Next work
1. Confirm Ubuntu workflow success and Windows solution CI.
2. Introduce platform-neutral application runtime with tests while preserving WPF behavior.
3. Add independent Avalonia app and virtual LIS2 smoke test.
4. Implement Linux providers, then UI feature parity and physical-device safety validation.

## Pull request gate
Open a PR against `main` only when a coherent, tested slice is ready. Keep this branch isolated until then.
