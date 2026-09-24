# TODO / Roadmap

This document tracks feature work for LIS2 Control Center. It is intentionally separate from the protocol notes: unresolved protocol questions belong in `docs/protocol.md` or `docs/reverse-engineering.md`.

## Next up

### Display pages / "Auto User"

- [ ] Turn the current page list into a richer sequence editor.
- [ ] Reorder pages easily (buttons and/or drag & drop).
- [ ] Configure duration per page.
- [ ] Enable/disable individual pages without deleting them.
- [ ] Make page duration drive the scheduler instead of relying on one global interval.
- [ ] Add convenient duplicate-page action.
- [ ] Improve page preview/editing workflow.
- [ ] Add optional conditional visibility based on source values.
- [ ] Add reusable page/display presets.

The original MCC's "Auto User" concept is useful inspiration here, but the new implementation should remain source-agnostic and more flexible.

### Scrolling / marquee

- [ ] Add overflow policy per display line/field: truncate, scroll/marquee, or static crop.
- [ ] Add configurable scroll speed and pause at start/end.
- [ ] Avoid unnecessary serial writes when the visible frame has not changed.
- [ ] Define sensible behavior when both lines scroll.
- [ ] Make long Winamp titles/artists a primary test case.

Historical MCC material indicates that track information could scroll while ordinary long text was more limited. The new renderer should make scrolling a general feature.

### Custom characters / CG Builder

- [ ] Add graphical editor for all eight programmable LIS2 character slots.
- [ ] 5-pixel-wide row editing with the LIS2 character RAM constraints visible in the UI.
- [ ] Live preview.
- [ ] Send one slot or all slots to the device.
- [ ] Save/load glyph sets as project-owned files.
- [ ] Allow custom glyphs to be referenced conveniently from page templates.
- [ ] Ship a small set of independently created example glyphs/icons.
- [ ] Validate every custom-character operation on real LIS2 hardware before treating it as stable.

The original MCC included a "CG Builder"; this is a confirmed useful product capability, not a reason to copy proprietary artwork.

## Hardware monitoring

- [x] LibreHardwareMonitor data source.
- [x] Live sensor browser.
- [x] Search/filter sensors.
- [x] Show values, units and template keys.
- [x] Assign a selected sensor directly to a fan channel.
- [ ] Add friendly aliases/favorites for frequently used sensors.
- [ ] Persist sensor favorites.
- [ ] Add ready-made display presets such as CPU, GPU, memory, temperatures and fans.
- [ ] Gracefully identify sensors that disappeared or changed after a hardware/driver update.
- [ ] Consider grouping sensors by hardware device in the browser.

Historical MCC versions integrated MBM5/SpeedFan and offered predefined monitoring choices. LIS2 Control Center should provide similar convenience without coupling the display engine to a particular monitoring backend.

## Fan control

- [x] Four fan channels.
- [x] Fixed output mode.
- [x] Curve mode.
- [x] Follow mode.
- [x] Minimum/maximum output.
- [x] Fail-safe output.
- [x] Missing/invalid sensor handling.
- [x] Hardware sensor selection.
- [ ] Add a clearer low-output safety warning/confirmation in the UI.
- [ ] Document the historical LIS2 channel rating reported as 12 V / 10 W / approximately 0.83 A per output, while clearly marking the source/verification status.
- [ ] Surface the original MCC's conservative low-speed behavior as historical guidance, not as an unverified electrical requirement.
- [ ] Add fan-curve graph/editor.
- [ ] Add live display of sensor input and resulting fan percentage per channel.
- [ ] Add optional hysteresis/smoothing to avoid output hunting.
- [ ] Add explicit emergency/fail-safe indication in the UI.
- [ ] Validate safe minimum outputs with real connected fans before choosing stronger defaults.

Do not infer electrical limits or safe fan-stop behavior solely from screenshots/reviews. Real-hardware verification remains authoritative.

## Winamp

- [x] Native 32-bit `gen_lis2.dll`.
- [x] Named-pipe transport.
- [x] Artist/title/album metadata.
- [x] Playback state.
- [x] Elapsed/duration.
- [x] Playlist position/count.
- [x] Bitrate/sample rate.
- [x] Winamp simulator.
- [x] Live Now Playing/status page in the Windows application.
- [ ] Improve stale/disconnect/reconnect diagnostics.
- [ ] Add optional display-page presets for Now Playing.
- [ ] Add configurable title/artist scrolling.
- [ ] Investigate a lightweight VU/level visualization.
- [ ] Investigate an optional small spectrum display using custom characters.
- [ ] Keep spectrum/VU optional: a 20x2 character VFD and serial update rate make high-resolution visualization a poor fit.
- [ ] Do not make the Winamp plugin responsible for LIS2 hardware access; it remains metadata/events only.

## Display runtime

- [x] 20x2 display frames.
- [x] Template renderer.
- [x] Page scheduler.
- [x] Event queue/overlay infrastructure.
- [x] Virtual display path.
- [x] Expose event overlays properly in the application UI.
- [x] Add test/demo event generation.
- [x] Add configurable event priority and lifetime.
- [ ] Add page transitions only where they make sense on a character VFD.
- [ ] Add general formatting helpers for numeric values, temperatures and percentages.
- [ ] Add fallback text for unavailable variables.
- [ ] Review thread safety of data-source snapshots while background sources update.

## LIS2 protocol / hardware validation

- [x] Serial transport at 19200 baud, 8N1.
- [x] Virtual transport.
- [x] Clear/reset command support.
- [x] Line 1/line 2 display commands.
- [x] Known brightness commands.
- [x] Four-channel fan command generation.
- [x] Known custom-character command generation.
- [ ] Validate the complete command set on physical LIS2 hardware.
- [ ] Resolve full-line trailing `00` behavior.
- [ ] Resolve display-off/brightness-off behavior.
- [ ] Validate all eight custom-character slots.
- [ ] Investigate safe device detection without sending speculative commands.
- [ ] Keep MPlay-specific commands out of automatic LIS2 initialization.
- [ ] Record every newly confirmed command with confidence level and test evidence.

## Application / UX

- [x] Windows WPF application.
- [x] Tray operation.
- [x] Windows autostart.
- [x] Dark theme.
- [x] Light theme.
- [x] Follow Windows app theme.
- [x] Theme-aware ComboBoxes and controls.
- [x] Startup diagnostic log for fatal startup problems.
- [ ] Theme the native Windows title bar consistently with the selected app theme.
- [ ] Review all controls in both Light and Dark themes.
- [ ] Improve first-run experience.
- [ ] Add explicit application/about/version information.
- [ ] Add settings export/import.
- [ ] Consider profile support for different machines/use cases.
- [ ] Add a safe "restore defaults" workflow.

## Virtual LIS2 / diagnostics

- [x] Virtual transport.
- [x] Virtual protocol interpreter/state.
- [x] Protocol tester.
- [x] Runtime diagnostics/logging.
- [ ] Expand virtual-device state display so fan, brightness, glyph and display state can be inspected together.
- [ ] Add command history with decoded protocol meaning.
- [ ] Add exportable diagnostic report.
- [ ] Add optional raw serial capture for hardware reverse-engineering sessions.
- [ ] Never send unknown/experimental commands automatically.

## Future data sources

These are deliberately lower priority than making the core LIS2 experience complete.

- [ ] Home Assistant.
- [ ] Plex.
- [ ] OctoPrint.
- [ ] Generic HTTP/JSON source.
- [ ] Server/Docker monitoring source.
- [ ] Possibly additional media-player integrations behind the same source abstraction.

## Engineering / quality

- [x] Windows CI.
- [x] Unit tests for Core, Display, Sources, Fans and Winamp mapping.
- [x] Self-contained Windows build artifact.
- [x] Native Winamp x86 build artifact.
- [ ] Add an application startup smoke test so WPF resource/startup regressions are caught by CI.
- [ ] Add tests around theme resource loading.
- [ ] Add tests for settings migration/defaulting.
- [ ] Add more fan safety edge-case tests.
- [ ] Review async/UI-thread boundaries.
- [ ] Review data-source concurrency and snapshot semantics.
- [ ] Add release/versioning workflow when the application reaches an alpha milestone.

## Research references to preserve

Useful historical material should be linked/documented rather than copied into the repository.

- TechPowerUp VLSystem LIS2 review, especially the installation/software screenshots:
  `https://www.techpowerup.com/review/vlsystem-lis2/2.html`
- Original MCC executable/binaries used for interoperability research must remain outside the repository.
- Findings derived from reverse engineering should be written down in `docs/reverse-engineering.md` with an appropriate confidence level.

## Non-goals / guardrails

- Do not redistribute proprietary VL System software or artwork.
- Do not blindly clone MCC's UI.
- Do not send unknown commands merely because they appear in an old binary.
- Do not merge LIS2 and MPlay protocol assumptions without hardware evidence.
- Do not sacrifice fan safety for visual convenience.
- Keep Winamp/media integrations separated from direct hardware access.
