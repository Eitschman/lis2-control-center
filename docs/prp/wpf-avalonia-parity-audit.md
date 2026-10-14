# WPF-to-Avalonia parity audit — all ten menus

2026-10-14. **Authority:** `src/LIS2.App/MainWindow.xaml` and `MainWindow.xaml.cs` (WPF). **Target:** `src/LIS2.App.Avalonia/MainWindow.axaml` and `MainWindow.*.cs`. Counts are named WPF controls per tab, not completion percentages. Source-level inventory only; no final manual UI acceptance.

| WPF menu | Named WPF controls | Source-based findings | Gap |
|---|---:|---|---|
| Dashboard | 7 | VFD preview, system status, quick links, four fan outputs. No connect/disconnect actions in WPF Dashboard; invented buttons reverted in 2cb3141. WPF manual fan actions differ from disabled Avalonia sliders. | Partial / requires control-by-control verification |
| Display | 11 | Quick display, glyph editor, ROM, brightness. Partial Avalonia implementation; interaction parity pending. | Partial / requires control-by-control verification |
| Pages | 17 | Page editor, presets, conditions, durations, overflow, scrolling, previews. Partial Avalonia implementation; behavioral parity pending. | Partial / requires control-by-control verification |
| Media | 36 | WPF Windows Media Player, Winamp, WMP Legacy, VU/spectrum and presets. No Avalonia MediaView. | Missing view |
| Events | 7 | WPF event editor, priority/duration, queue, warning test, clear and cancel. No Avalonia EventsView. | Missing view |
| Hardware | 7 | WPF sensor filter, aliases, favorites, assignment and presets. Substantial Avalonia work; not fully verified. | Partial / requires control-by-control verification |
| Fan Control | 18 | WPF manual and automatic fans, channel editor and curve preview. Avalonia has editor/simulation; safety and parity pending. | Partial / requires control-by-control verification |
| Settings | 5 | WPF transport, serial, theme, language, Windows startup. Avalonia transport settings; remainder pending. | Partial / requires control-by-control verification |
| Diagnostics | 4 | WPF diagnostic text, virtual state, history, logs, export. Avalonia partially implemented. | Partial / requires control-by-control verification |
| Home Assistant | 14 | WPF HA connection, entities/attributes, aliases, favorites and page creation. No Avalonia HomeAssistantView. | Missing view |

## Binding rules and acceptance

- No speculative UI, new shortcuts, features or alternative flows. WPF is the functional and visual target. Platform differences must be documented and approved, not silently invented.
- Before implementation in each tab, map every WPF control, handler, binding, default, state, validation, persistence and localization string to Avalonia. The initial inventory above is **not** the complete per-control mapping.
- Add automated regression tests for ported behavior and safety; require all shared and Avalonia Windows/Linux CI jobs green. Preserve serialized hardware writes, disabled physical fan output by default, and explicit authorization.
- No physical device tests available. No manual desktop acceptance until the full port; owner-supplied Pi screenshots are feedback only.
- Implement the three entirely missing views (Events, Home Assistant, Media) according to WPF source, then reconcile remaining seven views. Do not mark a menu complete without parity evidence.

## Corrected regression

The Dashboard Connect/Disconnect buttons from commit `50665f7` were not present in WPF and were reverted in `2cb3141`. Existing virtual connection actions in Settings are unchanged.
