# TODO / Roadmap

This file tracks **remaining work** for LIS2 Control Center as implemented on `main`. Completed areas are summarized where useful, but old planning items that have already become normal product functionality are no longer presented as future work.

Protocol uncertainties belong in `docs/protocol.md` / `docs/reverse-engineering.md`.

## Current implementation baseline

The following larger feature areas are already implemented:

- [x] Serial and virtual LIS2 transports.
- [x] 20x2 display runtime, page scheduler and differential display writes.
- [x] Page editor with reorder, duplicate, enable/disable, duration and live preview.
- [x] Ping-pong, marquee and truncate overflow modes with configurable timing.
- [x] Simple source-value conditional visibility.
- [x] Page presets: Clock, Status, CPU, GPU, Memory, Temperatures, Fans, Winamp Now Playing, VU and Spectrum.
- [x] Event/overlay queue with priority/lifetime and application UI.
- [x] Eight-slot custom-character manager and graphical CG Builder.
- [x] Save/load custom glyph sets and named template references.
- [x] Native uPD16314 character mapping including umlauts and common symbols.
- [x] Character-ROM/raw-byte testing UI.
- [x] LibreHardwareMonitor browser with grouping, search/filter, aliases, favorites and missing-sensor handling.
- [x] Four-channel fan control with Fixed, Curve, Follow, External and Off modes.
- [x] Fan curves, live values, min/max/fail-safe and hysteresis.
- [x] Native Winamp x86 plug-in, simulator, metadata, VU and spectrum telemetry.
- [x] Eight-level custom-glyph Winamp spectrum.
- [x] Home Assistant WebSocket source with entity browser, aliases/favorites and page creation.
- [x] Home Assistant entity attributes exposed as template values and browsable in the UI.
- [x] Tray/autostart, themes, localization and About/version UI.
- [x] Windows CI, unit tests, self-contained artifacts and WPF startup/tab smoke test.
- [x] Coalesced asynchronous display rendering.

## Display pages / runtime

- [x] Improve the **conditional visibility editor UX** with live variable selection and `=`, `!=`, `>`, `>=`, `<`, `<=` operators.
- [x] Add general template formatting helpers: number, percent, bytes, prefix/suffix and case conversion.
- [x] Add per-variable `fallback:` text for missing, `unknown` and `unavailable` values.
- [ ] Consider page transitions only if they produce a useful effect on a 20x2 character VFD.
- [ ] Review source snapshot/thread-safety semantics as more background/event-driven sources are added.

## Home Assistant

Implemented today:

- [x] WebSocket authentication and automatic reconnect.
- [x] Initial `get_states` snapshot and live `state_changed` events.
- [x] Search and domain filtering.
- [x] Entity aliases and favorites.
- [x] Entity state/unit/friendly-name variables.
- [x] All entity attributes exposed as `{HA.<entity>.attribute.<name>}`.
- [x] Alias-based attribute variables.
- [x] Attribute browser.
- [x] Direct display-page creation from entities and attributes.
- [x] Read-only integration by design.

Remaining:

- [ ] Validate behavior with a wider range of real HA integrations, especially entities with large/structured attributes.
- [ ] Consider friendlier formatting for JSON array/object attributes if real integrations make that useful.
- [ ] Consider secure OS-backed token storage instead of keeping the long-lived token in the application settings JSON.
- [ ] Keep service calls/control out of scope unless there is a concrete LIS2 use case.

## Hardware monitoring

Implemented:

- [x] LibreHardwareMonitor source and live sensor browser.
- [x] Search/filter and grouping by hardware.
- [x] Values, units and template keys.
- [x] Aliases and favorites with persistence.
- [x] Configured-but-disappeared sensors remain visible as unavailable.
- [x] Direct fan-channel assignment.
- [x] CPU/GPU/Memory/Temperatures/Fans display presets.

Remaining:

- [ ] Continue validating sensor identity stability across LibreHardwareMonitor/driver updates.
- [ ] Improve formatting/fallback helpers at the display layer rather than adding backend-specific formatting.

## Fan control / safety

Implemented:

- [x] Four channels.
- [x] Fixed, Curve, Follow, External and Off modes.
- [x] Minimum/maximum/fail-safe outputs.
- [x] Missing/invalid sensor handling.
- [x] Hardware sensor selection.
- [x] Fan-curve editor/preview.
- [x] Live input/output values.
- [x] Optional hysteresis.

Remaining:

- [ ] Add a clearer low-output safety warning/confirmation in the UI.
- [ ] Add explicit emergency/fail-safe indication in the UI.
- [ ] Document the historical reported channel rating of 12 V / 10 W / approximately 0.83 A while clearly identifying the source and verification status.
- [ ] Preserve original MCC low-speed behavior only as historical guidance, not as an electrical requirement.
- [ ] Validate safe minimum outputs and stop behavior on real connected fans before choosing stronger defaults.

## Winamp

Implemented:

- [x] Native x86 `gen_lis2.dll`.
- [x] Named-pipe transport.
- [x] Playback state and metadata.
- [x] Elapsed/duration, playlist position/count, bitrate/sample rate.
- [x] Winamp simulator.
- [x] Stale/disconnect/reconnect handling.
- [x] Now Playing preset and scrolling.
- [x] VU telemetry/display.
- [x] 20-band analyzer/spectrum telemetry.
- [x] Eight-level custom-glyph VFD spectrum preset.

Remaining:

- [ ] Keep VU/spectrum deliberately modest in update rate; the LIS2 is a character VFD, not a high-frame-rate visualizer.
- [ ] Keep the Winamp plug-in metadata/telemetry-only. It must not gain direct LIS2 hardware access.

## Custom characters / character ROM

Implemented:

- [x] Graphical editor for all eight 5x8 slots.
- [x] Live preview and one/all-slot programming.
- [x] Glyph-set save/load.
- [x] Template references.
- [x] Built-in independently created glyph sets.
- [x] Spectrum bar glyph set.
- [x] Native uPD16314 ROM-code-002 mapping for Latin-1 and selected useful Unicode symbols.
- [x] Raw 40-byte ROM range tester.

Remaining:

- [ ] Validate all eight custom-character slots on physical LIS2 hardware.
- [ ] Validate the newly mapped extended character ROM against the physical display and record any deviations.
- [ ] Record confirmed physical-ROM findings in `docs/protocol.md`.

## LIS2 protocol / hardware validation

Implemented in production code:

- [x] Serial 19200 baud, 8N1.
- [x] Virtual transport/interpreter.
- [x] `A0` clear/reset.
- [x] `A1/A2 <column> A7 ...` line writes.
- [x] Brightness commands for 100/75/50/25%.
- [x] `AE F0 ...` four-channel fan writes.
- [x] `AB <slot> <row> <bitmap>` custom-character programming.
- [x] Native display-character encoding.
- [x] Raw display-byte writes for explicit testing.

Still unresolved / requiring real hardware:

- [ ] Validate the complete supported command set on physical LIS2 hardware.
- [ ] Resolve MCC full-line trailing `00` behavior.
- [ ] Resolve display-off/brightness-off behavior.
- [ ] Validate all custom-character operations.
- [ ] Investigate safe device detection without speculative writes.
- [ ] Determine whether the device returns useful identification/version information.
- [ ] Keep MPlay-specific commands out of automatic LIS2 initialization.
- [ ] Record each newly confirmed command with confidence level and test evidence.

## Application / UX

Implemented:

- [x] WPF application and tray operation.
- [x] Windows autostart.
- [x] Light, Dark and System themes.
- [x] Runtime localization: System, English, German, French, Turkish and Russian with English fallback.
- [x] Localized tray menu and LIS2 app/tray/taskbar icon.
- [x] Startup diagnostics.
- [x] About/version information.
- [x] Hardware and Home Assistant browser/table layout cleanup.

Remaining:

- [x] Theme the native Windows title bar consistently with the selected application theme.
- [x] Complete a full Light/Dark control review, including input, selection, focus, disabled and resizable-pane states.
- [ ] Continue UI polish only when real-world data exposes a concrete layout problem.
- [ ] Consider profile support only if multiple machine/use-case configurations actually need it.

Deliberately omitted:

- [x] First-run wizard — **not needed**.
- [x] Settings import/export — **not needed**.
- [x] Restore-defaults workflow — **not needed**.

## Virtual LIS2 / diagnostics

Implemented:

- [x] Virtual transport and protocol interpreter/state.
- [x] Protocol tester.
- [x] Runtime diagnostics/logging.
- [x] Virtual decoding of native international display characters.

Remaining:

- [x] Expand virtual-device diagnostics so display, fan, brightness and glyph state can be inspected together.
- [x] Add decoded command history for the virtual transport.
- [x] Add an exportable diagnostic report.
- [ ] Add optional raw serial capture for reverse-engineering sessions.
- [ ] Never send unknown/experimental commands automatically.

## Additional data sources

Home Assistant is the preferred **universal integration layer** for values that already exist in HA. A dedicated LIS2 source must provide a concrete capability beyond simply exposing the same values again.

Examples that should normally come through Home Assistant include:

- OctoPrint printer/job/temperature values
- Plex media/player values
- Beszel, Docker and server monitoring
- DWD/weather data
- network/device integrations
- Zigbee and other smart-home sensors

Consequently:

- [x] OctoPrint dedicated source — **not needed; use Home Assistant**.
- [x] Generic Docker/server-monitoring source — **not needed; use Home Assistant**.
- [x] Plex dedicated source — **not needed; use Home Assistant**.
- [ ] Generic HTTP/JSON source — only if there is a real data source that cannot reasonably be exposed through HA.
- [ ] Additional media-player integrations — only for capabilities that justify a dedicated source.

Do not add one-off integrations merely because a system has an API. Prefer the existing HA source whenever HA already exposes the required state and attributes.

## Engineering / quality

Implemented:

- [x] Windows CI.
- [x] Core, Display, Sources, Fans and Winamp tests.
- [x] Self-contained Windows x64 application artifact.
- [x] Self-contained protocol-tester artifact.
- [x] Native Winamp x86 artifact.
- [x] WPF startup/tab smoke test.
- [x] Serialized/coalesced display rendering.

Remaining:

- [x] Exercise Light and Dark theme resource loading in the WPF startup/tab smoke test.
- [ ] Add tests for settings migration/defaulting.
- [ ] Add more fan safety edge-case tests.
- [x] Review data-source snapshot semantics and add concurrent snapshot publication coverage.
- [ ] Add a release/versioning workflow when the project reaches an explicit release milestone.

## Research / guardrails

- Do not redistribute proprietary VL System software, installers or artwork.
- Do not blindly clone MCC's UI.
- Do not send unknown protocol commands because they appear in an old binary.
- Keep LIS2 and MPlay assumptions separate.
- Keep fan safety independent from display/media/network integrations.
- Keep the Winamp plug-in separated from hardware access.
- Treat physical LIS2 validation as authoritative for protocol behavior.

Historical references and reverse-engineering evidence belong in `docs/reverse-engineering.md`.
