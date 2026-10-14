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

## Detailed WPF XAML control/event inventory (2026-10-14)

This is an **exact-name inventory**, not a feature parity verdict. Avalonia often renames equivalent controls and binds handlers through code; a `different name` entry **does not prove missing functionality**. The three entirely missing views are separately established by the absence of MediaView, EventsView and HomeAssistantView. Every entry below must subsequently be mapped semantically to Avalonia code-behind and verified with tests. Source: WPF `MainWindow.xaml`, target Avalonia `MainWindow.axaml`.

### Dashboard

WPF named controls: **7**; exact-name matches in Avalonia XAML: **7**; distinct WPF event handler names: **2**. Different names require semantic mapping, not automatic classification.

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `PreviewLine1` | Exact name exists | Requires handler and behavior review |
| `PreviewLine2` | Exact name exists | Requires handler and behavior review |
| `VirtualStateText` | Exact name exists | Requires handler and behavior review |
| `DashboardFan1Slider` | Exact name exists | Requires handler and behavior review |
| `DashboardFan2Slider` | Exact name exists | Requires handler and behavior review |
| `DashboardFan3Slider` | Exact name exists | Requires handler and behavior review |
| `DashboardFan4Slider` | Exact name exists | Requires handler and behavior review |

**WPF XAML event handlers:** `Navigation_Click`, `DashboardFans_Click`.

### Display

WPF named controls: **11**; exact-name matches in Avalonia XAML: **4**; distinct WPF event handler names: **19**. Different names require semantic mapping, not automatic classification.

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `Line1TextBox` | Exact name exists | Requires handler and behavior review |
| `Line2TextBox` | Exact name exists | Requires handler and behavior review |
| `CharacterRomStartTextBox` | Exact name exists | Requires handler and behavior review |
| `CharacterRomRangeText` | Exact name exists | Requires handler and behavior review |
| `ProgramCustomGlyphsOnConnectCheckBox` | Different name / not found | Requires handler and behavior review |
| `CustomGlyphSlotComboBox` | Different name / not found | Requires handler and behavior review |
| `CustomGlyphNameTextBox` | Different name / not found | Requires handler and behavior review |
| `CustomGlyphTemplateKeyText` | Different name / not found | Requires handler and behavior review |
| `CustomGlyphRowsTextBox` | Different name / not found | Requires handler and behavior review |
| `CustomGlyphPreview` | Different name / not found | Requires handler and behavior review |
| `CustomGlyphLinePreview` | Different name / not found | Requires handler and behavior review |

**WPF XAML event handlers:** `SendLine1_Click`, `SendLine2_Click`, `RenderBoth_Click`, `Clear_Click`, `SendSpecialCharacterTest_Click`, `CharacterRomPrevious_Click`, `SendCharacterRomRange_Click`, `CharacterRomNext_Click`, `ProgramCustomGlyphsOnConnectChanged`, `CustomGlyphSlotChanged`, `CustomGlyphNameChanged`, `CustomGlyphRowsChanged`, `SaveCustomGlyph_Click`, `SendCustomGlyph_Click`, `SendAllCustomGlyphs_Click`, `LoadCustomGlyphSet_Click`, `SaveCustomGlyphSet_Click`, `LoadExampleGlyphSet_Click`, `Brightness_Click`.

### Pages

WPF named controls: **17**; exact-name matches in Avalonia XAML: **0**; distinct WPF event handler names: **12**. Different names require semantic mapping, not automatic classification.

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `PagePresetComboBox` | Different name / not found | Requires handler and behavior review |
| `PagesListBox` | Different name / not found | Requires handler and behavior review |
| `PageNameTextBox` | Different name / not found | Requires handler and behavior review |
| `PageLine1TextBox` | Different name / not found | Requires handler and behavior review |
| `PageLine2TextBox` | Different name / not found | Requires handler and behavior review |
| `PageDurationTextBox` | Different name / not found | Requires handler and behavior review |
| `PageRefreshIntervalTextBox` | Different name / not found | Requires handler and behavior review |
| `PageVisibilityKeyComboBox` | Different name / not found | Requires handler and behavior review |
| `PageVisibilityOperatorComboBox` | Different name / not found | Requires handler and behavior review |
| `PageVisibilityValueTextBox` | Different name / not found | Requires handler and behavior review |
| `PageVisibilityTextBox` | Different name / not found | Requires handler and behavior review |
| `PageLine1OverflowComboBox` | Different name / not found | Requires handler and behavior review |
| `PageLine2OverflowComboBox` | Different name / not found | Requires handler and behavior review |
| `PageScrollSpeedTextBox` | Different name / not found | Requires handler and behavior review |
| `PageEdgePauseTextBox` | Different name / not found | Requires handler and behavior review |
| `PageEditorPreviewLine1` | Different name / not found | Requires handler and behavior review |
| `PageEditorPreviewLine2` | Different name / not found | Requires handler and behavior review |

**WPF XAML event handlers:** `CreatePagePreset_Click`, `PagesListBox_SelectionChanged`, `PageEnabledChanged`, `AddPage_Click`, `DuplicatePage_Click`, `DeletePage_Click`, `MovePageUp_Click`, `MovePageDown_Click`, `PageEditorChanged`, `BuildVisibilityExpression_Click`, `SavePage_Click`, `TestEvent_Click`.

### Media

WPF named controls: **36**; exact-name matches in Avalonia XAML: **0**; distinct WPF event handler names: **2**. **Avalonia view absent.**

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `WindowsMediaStatusText` | Different name / not found | Requires handler and behavior review |
| `WindowsMediaTitleText` | Different name / not found | Requires handler and behavior review |
| `WindowsMediaArtistText` | Different name / not found | Requires handler and behavior review |
| `WindowsMediaAlbumText` | Different name / not found | Requires handler and behavior review |
| `WindowsMediaPlaybackText` | Different name / not found | Requires handler and behavior review |
| `WindowsMediaSourceText` | Different name / not found | Requires handler and behavior review |
| `WindowsMediaTimeText` | Different name / not found | Requires handler and behavior review |
| `WinampStatusText` | Different name / not found | Requires handler and behavior review |
| `WinampTitleText` | Different name / not found | Requires handler and behavior review |
| `WinampArtistText` | Different name / not found | Requires handler and behavior review |
| `WinampAlbumText` | Different name / not found | Requires handler and behavior review |
| `WinampPlaybackText` | Different name / not found | Requires handler and behavior review |
| `WinampTimeText` | Different name / not found | Requires handler and behavior review |
| `WinampPlaylistText` | Different name / not found | Requires handler and behavior review |
| `WinampAudioText` | Different name / not found | Requires handler and behavior review |
| `WinampTelemetryStatusText` | Different name / not found | Requires handler and behavior review |
| `WinampVuLeftBar` | Different name / not found | Requires handler and behavior review |
| `WinampVuLeftText` | Different name / not found | Requires handler and behavior review |
| `WinampVuRightBar` | Different name / not found | Requires handler and behavior review |
| `WinampVuRightText` | Different name / not found | Requires handler and behavior review |
| `WinampSpectrumText` | Different name / not found | Requires handler and behavior review |
| `WinampConnectionDetailText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyStatusText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyTitleText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyArtistText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyAlbumText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyPlaybackText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyTimeText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyPlaylistText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyVisualizationStatusText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyVuLeftBar` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyVuLeftText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyVuRightBar` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyVuRightText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacySpectrumText` | Different name / not found | Requires handler and behavior review |
| `WmpLegacyConnectionDetailText` | Different name / not found | Requires handler and behavior review |

**WPF XAML event handlers:** `CreateWindowsMediaPreset_Click`, `CreateWinampPreset_Click`.

### Events

WPF named controls: **7**; exact-name matches in Avalonia XAML: **0**; distinct WPF event handler names: **5**. **Avalonia view absent.**

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `EventLine1TextBox` | Different name / not found | Requires handler and behavior review |
| `EventLine2TextBox` | Different name / not found | Requires handler and behavior review |
| `EventPriorityComboBox` | Different name / not found | Requires handler and behavior review |
| `EventDurationTextBox` | Different name / not found | Requires handler and behavior review |
| `EventQueueSummaryText` | Different name / not found | Requires handler and behavior review |
| `EventQueueListBox` | Different name / not found | Requires handler and behavior review |
| `SelectedEventText` | Different name / not found | Requires handler and behavior review |

**WPF XAML event handlers:** `AddEvent_Click`, `TestWarningEvent_Click`, `ClearEvents_Click`, `EventQueueListBox_SelectionChanged`, `CancelSelectedEvent_Click`.

### Hardware

WPF named controls: **7**; exact-name matches in Avalonia XAML: **0**; distinct WPF event handler names: **6**. Different names require semantic mapping, not automatic classification.

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `HardwareSummaryText` | Different name / not found | Requires handler and behavior review |
| `HardwareSearchTextBox` | Different name / not found | Requires handler and behavior review |
| `HardwareTypeComboBox` | Different name / not found | Requires handler and behavior review |
| `HardwareSensorsListBox` | Different name / not found | Requires handler and behavior review |
| `SelectedHardwareSensorText` | Different name / not found | Requires handler and behavior review |
| `HardwareAliasTextBox` | Different name / not found | Requires handler and behavior review |
| `HardwareFavoriteCheckBox` | Different name / not found | Requires handler and behavior review |

**WPF XAML event handlers:** `HardwareFilterChanged`, `RefreshHardware_Click`, `HardwareSensorsListBox_SelectionChanged`, `SaveHardwarePreference_Click`, `AssignHardwareSensorToFan_Click`, `CreateHardwarePreset_Click`.

### Fan Control

WPF named controls: **18**; exact-name matches in Avalonia XAML: **0**; distinct WPF event handler names: **6**. Different names require semantic mapping, not automatic classification.

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `AutomaticFanControlCheckBox` | Different name / not found | Requires handler and behavior review |
| `Fan1Slider` | Different name / not found | Requires handler and behavior review |
| `Fan2Slider` | Different name / not found | Requires handler and behavior review |
| `Fan3Slider` | Different name / not found | Requires handler and behavior review |
| `Fan4Slider` | Different name / not found | Requires handler and behavior review |
| `FanChannelsListBox` | Different name / not found | Requires handler and behavior review |
| `FanNameTextBox` | Different name / not found | Requires handler and behavior review |
| `FanModeComboBox` | Different name / not found | Requires handler and behavior review |
| `FanSensorComboBox` | Different name / not found | Requires handler and behavior review |
| `FanFixedTextBox` | Different name / not found | Requires handler and behavior review |
| `FanMinimumTextBox` | Different name / not found | Requires handler and behavior review |
| `FanMaximumTextBox` | Different name / not found | Requires handler and behavior review |
| `FanFailSafeTextBox` | Different name / not found | Requires handler and behavior review |
| `FanCurveTextBox` | Different name / not found | Requires handler and behavior review |
| `FanHysteresisTextBox` | Different name / not found | Requires handler and behavior review |
| `FanAllowStopCheckBox` | Different name / not found | Requires handler and behavior review |
| `FanLiveStatusText` | Different name / not found | Requires handler and behavior review |
| `FanCurvePreviewCanvas` | Different name / not found | Requires handler and behavior review |

**WPF XAML event handlers:** `AutomaticFanControlChanged`, `Fans_Click`, `FanChannelsListBox_SelectionChanged`, `RefreshFanSensors_Click`, `FanCurveTextChanged`, `SaveFanChannel_Click`.

### Settings

WPF named controls: **5**; exact-name matches in Avalonia XAML: **0**; distinct WPF event handler names: **6**. Different names require semantic mapping, not automatic classification.

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `TransportModeComboBox` | Different name / not found | Requires handler and behavior review |
| `PortComboBox` | Different name / not found | Requires handler and behavior review |
| `ThemeModeComboBox` | Different name / not found | Requires handler and behavior review |
| `LanguageModeComboBox` | Different name / not found | Requires handler and behavior review |
| `StartWithWindowsCheckBox` | Different name / not found | Requires handler and behavior review |

**WPF XAML event handlers:** `TransportModeChanged`, `RefreshPorts_Click`, `ApplyDeviceSettings_Click`, `ThemeModeChanged`, `LanguageModeChanged`, `StartWithWindowsChanged`.

### Diagnostics

WPF named controls: **4**; exact-name matches in Avalonia XAML: **0**; distinct WPF event handler names: **2**. Different names require semantic mapping, not automatic classification.

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `DiagnosticsTextBox` | Different name / not found | Requires handler and behavior review |
| `VirtualLis2StateTextBox` | Different name / not found | Requires handler and behavior review |
| `CommandHistoryTextBox` | Different name / not found | Requires handler and behavior review |
| `LogTextBox` | Different name / not found | Requires handler and behavior review |

**WPF XAML event handlers:** `ExportDiagnostics_Click`, `RefreshDiagnostics_Click`.

### Home Assistant

WPF named controls: **14**; exact-name matches in Avalonia XAML: **0**; distinct WPF event handler names: **11**. **Avalonia view absent.**

| WPF control | Avalonia exact name | Mapping/verification |
|---|---|---|
| `HomeAssistantEnabledCheckBox` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantUrlTextBox` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantTokenPasswordBox` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantSearchTextBox` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantDomainComboBox` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantStatusText` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantEntitiesListBox` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantDetailBorder` | Different name / not found | Requires handler and behavior review |
| `SelectedHomeAssistantEntityText` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantAliasTextBox` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantFavoriteCheckBox` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantEntityActionsPanel` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantAttributeActionsPanel` | Different name / not found | Requires handler and behavior review |
| `HomeAssistantAttributesListBox` | Different name / not found | Requires handler and behavior review |

**WPF XAML event handlers:** `TestHomeAssistant_Click`, `SaveHomeAssistant_Click`, `HomeAssistantFilterChanged`, `HomeAssistantEntitiesListBox_SelectionChanged`, `HomeAssistantEntitiesListBox_MouseDoubleClick`, `CopyHomeAssistantEntityKey_Click`, `SaveHomeAssistantEntity_Click`, `CreateHomeAssistantPage_Click`, `CopyHomeAssistantAttributeKey_Click`, `CreateHomeAssistantAttributePage_Click`, `HomeAssistantAttributesListBox_MouseDoubleClick`.

## Interpretation and next audit gate

- The tables record actual WPF control identifiers and XAML-wired event handlers, not all unnamed buttons, bindings, implicit events, timers or code-behind actions.
- Follow-up must map each WPF item to an actual Avalonia control/handler (including renamed equivalents), compare defaults, persistence, validation, localization and states, and provide tests. Do not claim feature absence merely because names differ.
- No WPF source changes; no invented UI. Hardware behavior and owner deferred manual testing restrictions remain binding.
