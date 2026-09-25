# Release process

LIS2 Control Center uses two Windows GitHub Actions workflows.

## Continuous integration

Every push and pull request to `main`:

1. restores and builds the complete .NET 8 solution;
2. runs all unit, integration and application reliability tests;
3. runs the WPF startup/tab smoke test;
4. publishes development artifacts on non-pull-request builds;
5. builds the native x86 Winamp plug-in.

## Tagged releases

Push a semantic-version tag beginning with `v`, for example:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The release workflow validates the version, rebuilds and retests the complete solution, runs the WPF smoke test, publishes self-contained Windows x64 binaries, builds the x86 Winamp plug-in and creates a GitHub Release.

Release assets:

- `LIS2-Control-Center-<version>-win-x64.zip` — portable application package;
- `LIS2-ProtocolTester-<version>-win-x64.zip` — portable protocol tester;
- `gen_lis2.dll` — native 32-bit Winamp general-purpose plug-in;
- `SHA256SUMS.txt` — SHA-256 checksums for all release assets.

The application version is injected from the Git tag during the release build.

## Distribution policy

The portable ZIP is the primary distribution format before the first hardware-validated stable release. It avoids installer-specific side effects while the application and protocol behavior are still being validated on physical LIS2 hardware.

An installer may be added later if it provides concrete value such as optional Winamp plug-in deployment, Start-menu integration or managed uninstall. It is not required for the first release candidate.

## Release readiness

Before creating a release tag:

- the normal `main` build must be green;
- no known software-only regression may remain;
- unresolved protocol behavior must be documented rather than guessed;
- hardware-specific claims must remain explicitly unverified until tested on a physical LIS2.
