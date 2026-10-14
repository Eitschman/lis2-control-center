# WPF → Avalonia: vollständige XAML-Control-/Handler-Traceability

Stand 2026-10-14. **WPF ist führend**. Quellen: WPF `MainWindow.xaml`, `MainWindow.xaml.cs`; Avalonia `MainWindow.axaml`, `MainWindow.axaml.cs` und die partiellen Dateien für Pages, Fans, Hardware, Home Assistant und Diagnostics. **Dies ist eine vollständige Erfassung der benannten WPF-Tab-Controls und direkt im XAML referenzierten Event-Handler, nicht der Nachweis vollständiger Funktionsparität.** Unbenannte Controls, implizite Bindings, Timer, Hintergrundquellen, Lokalisierung und weitere Codepfade benötigen gesonderte semantische Prüfung.

**Bewertung:** Exakt/umbenannt bedeutet UI-Nachweis, nicht gleiches Verhalten. 'Anderer Menüpunkt' bedeutet keine WPF-Menüparität. 'Keine Zuordnung' ist eine offene oder fehlende Funktion, keine Vermutung.

## Dashboard

WPF VFD-Preview, Status, Navigation und vier **aktive** manuelle Fan-Slider mit Apply. Avalonia-Slider read-only: funktionale Abweichung; keine WPF-Dashboard-Connect-Buttons.

**WPF:** 7 benannte Controls, 2 XAML-Handler. Avalonia-View vorhanden, Parität offen.

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `PreviewLine1` | `PreviewLine1` | Exakter UI-Name |
| `PreviewLine2` | `PreviewLine2` | Exakter UI-Name |
| `VirtualStateText` | `VirtualStateText` | Exakter UI-Name |
| `DashboardFan1Slider` | `DashboardFan1Slider` | Exakter UI-Name |
| `DashboardFan2Slider` | `DashboardFan2Slider` | Exakter UI-Name |
| `DashboardFan3Slider` | `DashboardFan3Slider` | Exakter UI-Name |
| `DashboardFan4Slider` | `DashboardFan4Slider` | Exakter UI-Name |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `Navigation_Click` | `TryParse`, `UpdateNavigationSelection` | `NavigateTo` – Codebeleg, Verhaltensgleichheit offen |
| `DashboardFans_Click` | `ApplyManualFanValuesAsync`, `GetSliderPercent` | Keine gesicherte 1:1-Implementierung |

## Display

WPF: Send 1/2/both, Clear, Sonderzeichen, ROM-Blöcke, Glyph-Slot/Name/Bitmap, automatische Programmierung, Import/Export, Helligkeit. Avalonia-UI teilweise vorhanden, Fehler-/Persistenzpfade prüfen.

**WPF:** 11 benannte Controls, 19 XAML-Handler. Avalonia-View vorhanden, Parität offen.

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `Line1TextBox` | `Line1TextBox` | Exakter UI-Name |
| `Line2TextBox` | `Line2TextBox` | Exakter UI-Name |
| `CharacterRomStartTextBox` | `CharacterRomStartTextBox` | Exakter UI-Name |
| `CharacterRomRangeText` | `CharacterRomRangeText` | Exakter UI-Name |
| `ProgramCustomGlyphsOnConnectCheckBox` | `ProgramGlyphsOnConnect` | Umbenanntes UI; Semantik offen |
| `CustomGlyphSlotComboBox` | `GlyphSlot` | Umbenanntes UI; Semantik offen |
| `CustomGlyphNameTextBox` | `GlyphName` | Umbenanntes UI; Semantik offen |
| `CustomGlyphTemplateKeyText` | `GlyphTemplateKey` | Umbenanntes UI; Semantik offen |
| `CustomGlyphRowsTextBox` | `GlyphBitmapEditor` | Umbenanntes UI; Semantik offen |
| `CustomGlyphPreview` | `GlyphPixelPreview` | Umbenanntes UI; Semantik offen |
| `CustomGlyphLinePreview` | `GlyphPixelPreview` | Umbenanntes UI; Semantik offen |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `SendLine1_Click` | `Create`, `WriteFrameAsync`, `RefreshPreview` | `SendLine1` – Codebeleg, Verhaltensgleichheit offen |
| `SendLine2_Click` | `Create`, `WriteFrameAsync`, `RefreshPreview` | `SendLine2` – Codebeleg, Verhaltensgleichheit offen |
| `RenderBoth_Click` | `Create`, `WriteFrameAsync`, `RefreshPreview` | `RenderBoth` – Codebeleg, Verhaltensgleichheit offen |
| `Clear_Click` | `RequireDevice`, `ClearAsync`, `Reset`, `Create`, `RefreshPreview` | `ClearDisplay` – Codebeleg, Verhaltensgleichheit offen |
| `SendSpecialCharacterTest_Click` | `Create`, `WriteFrameAsync`, `RefreshPreview` | `SendSpecialCharacterTest` – Codebeleg, Verhaltensgleichheit offen |
| `CharacterRomPrevious_Click` | `ParseCharacterRomStart`, `SetCharacterRomStart`, `Max` | `CharacterRomPrevious` – Codebeleg, Verhaltensgleichheit offen |
| `SendCharacterRomRange_Click` | `ParseCharacterRomStart`, `Range`, `Select`, `ToArray`, `RequireDevice`, `WriteRawDisplayBytesAsync` | `SendCharacterRomRange` – Codebeleg, Verhaltensgleichheit offen |
| `CharacterRomNext_Click` | `ParseCharacterRomStart`, `SetCharacterRomStart`, `Min` | `CharacterRomNext` – Codebeleg, Verhaltensgleichheit offen |
| `ProgramCustomGlyphsOnConnectChanged` | `SaveAsync` | `ProgramGlyphsOnConnect` – Codebeleg, Verhaltensgleichheit offen |
| `CustomGlyphSlotChanged` | `LoadSelectedGlyphIntoEditor` | `GlyphSlot` – Codebeleg, Verhaltensgleichheit offen |
| `CustomGlyphNameChanged` | `RefreshCustomGlyphTemplateKey`, `RefreshGlyphPreview`, `ReadGlyphEditor` | Keine gesicherte 1:1-Implementierung |
| `CustomGlyphRowsChanged` | `RefreshGlyphPreview`, `ReadGlyphEditor` | `GlyphBitmapEditor` – Codebeleg, Verhaltensgleichheit offen |
| `SaveCustomGlyph_Click` | `ReadGlyphEditor`, `Trim`, `SaveAsync`, `RefreshCustomGlyphTemplateKey`, `RefreshGlyphPreview` | Keine gesicherte 1:1-Implementierung |
| `SendCustomGlyph_Click` | `ReadGlyphEditor`, `InvalidOperationException`, `ProgramSlotAsync`, `RefreshGlyphPreview` | `SendGlyphSlot` – Codebeleg, Verhaltensgleichheit offen |
| `SendAllCustomGlyphs_Click` | `InvalidOperationException`, `ProgramAllAsync`, `Select`, `ToArray` | `SendAllGlyphs` – Codebeleg, Verhaltensgleichheit offen |
| `LoadCustomGlyphSet_Click` | `sets`, `JSON`, `files`, `ShowDialog`, `ReadAllTextAsync`, `InvalidOperationException` | Keine gesicherte 1:1-Implementierung |
| `SaveCustomGlyphSet_Click` | `sets`, `JSON`, `files`, `ShowDialog`, `GetFileNameWithoutExtension`, `CloneGlyphs` | Keine gesicherte 1:1-Implementierung |
| `LoadExampleGlyphSet_Click` | `ApplyGlyphSetAsync`, `CreateExampleSet` | Keine gesicherte 1:1-Implementierung |
| `Brightness_Click` | `InvalidOperationException`, `Parse`, `SaveAsync`, `RequireDevice`, `SetBrightnessAsync` | `Brightness100` – Codebeleg, Verhaltensgleichheit offen |

## Pages

WPF: Presets, CRUD, Reihenfolge, Aktivierung, Bedingungen, Timing, Overflow, Vorschau, Event-Test. Avalonia-Äquivalente umbenannt; Detailzustände prüfen.

**WPF:** 17 benannte Controls, 12 XAML-Handler. Avalonia-View vorhanden, Parität offen.

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `PagePresetComboBox` | — | Keine gesicherte Zuordnung |
| `PagesListBox` | `PagesList` | Umbenanntes UI; Semantik offen |
| `PageNameTextBox` | `PageName` | Umbenanntes UI; Semantik offen |
| `PageLine1TextBox` | `PageLine1` | Umbenanntes UI; Semantik offen |
| `PageLine2TextBox` | `PageLine2` | Umbenanntes UI; Semantik offen |
| `PageDurationTextBox` | `PageDuration` | Umbenanntes UI; Semantik offen |
| `PageRefreshIntervalTextBox` | `PageRefresh` | Umbenanntes UI; Semantik offen |
| `PageVisibilityKeyComboBox` | `PageConditionKey` | Umbenanntes UI; Semantik offen |
| `PageVisibilityOperatorComboBox` | `PageConditionOperator` | Umbenanntes UI; Semantik offen |
| `PageVisibilityValueTextBox` | `PageConditionValue` | Umbenanntes UI; Semantik offen |
| `PageVisibilityTextBox` | `PageVisibility` | Umbenanntes UI; Semantik offen |
| `PageLine1OverflowComboBox` | `PageOverflow1` | Umbenanntes UI; Semantik offen |
| `PageLine2OverflowComboBox` | `PageOverflow2` | Umbenanntes UI; Semantik offen |
| `PageScrollSpeedTextBox` | `PageScrollStep` | Umbenanntes UI; Semantik offen |
| `PageEdgePauseTextBox` | `PageScrollPause` | Umbenanntes UI; Semantik offen |
| `PageEditorPreviewLine1` | `PagePreview1` | Umbenanntes UI; Semantik offen |
| `PageEditorPreviewLine2` | `PagePreview2` | Umbenanntes UI; Semantik offen |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `CreatePagePreset_Click` | `CreatePageFromPresetAsync` | Keine gesicherte 1:1-Implementierung |
| `PagesListBox_SelectionChanged` | `ApplyVisibilityExpressionToBuilder`, `SelectComboBoxTag`, `RefreshPageEditorPreview` | `PagesList` – Codebeleg, Verhaltensgleichheit offen |
| `PageEnabledChanged` | `PersistPagesAsync`, `LoadPagesIntoRuntime`, `RenderRuntimePageAsync` | `PageEnabled` – Codebeleg, Verhaltensgleichheit offen |
| `AddPage_Click` | `Add`, `PersistPagesAsync`, `BindPages` | `PageAdd` – Codebeleg, Verhaltensgleichheit offen |
| `DuplicatePage_Click` | `NewGuid`, `IndexOf`, `Insert`, `PersistPagesAsync`, `BindPages`, `LoadPagesIntoRuntime` | `PageDuplicate` – Codebeleg, Verhaltensgleichheit offen |
| `DeletePage_Click` | `Remove`, `EnsureDefaults`, `PersistPagesAsync`, `BindPages`, `LoadPagesIntoRuntime` | `PageDelete` – Codebeleg, Verhaltensgleichheit offen |
| `MovePageUp_Click` | `RemoveAt`, `Insert`, `PersistPagesAsync`, `BindPages`, `LoadPagesIntoRuntime` | `PageUp` – Codebeleg, Verhaltensgleichheit offen |
| `MovePageDown_Click` | `RemoveAt`, `Insert`, `PersistPagesAsync`, `BindPages`, `LoadPagesIntoRuntime` | `PageDown` – Codebeleg, Verhaltensgleichheit offen |
| `PageEditorChanged` | `RefreshPageEditorPreview` | `RefreshPagePreview` – Codebeleg, Verhaltensgleichheit offen |
| `BuildVisibilityExpression_Click` | `Trim`, `GetComboBoxTag` | Keine gesicherte 1:1-Implementierung |
| `SavePage_Click` | `InvalidOperationException`, `TryParse`, `Trim`, `operator`, `GetComboBoxTag`, `PersistPagesAsync` | `PageSave` – Codebeleg, Verhaltensgleichheit offen |
| `TestEvent_Click` | `QueueDisplayEventAsync`, `NewGuid`, `FromSeconds` | `PageTest` – Codebeleg, Verhaltensgleichheit offen |

## Media

WPF: Windows Media, Winamp und WMP Legacy; Metadaten, Playback, VU/Spectrum, Presets. Avalonia hat keine MediaView.

**WPF:** 36 benannte Controls, 2 XAML-Handler. **Keine eigenständige Avalonia-View.**

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `WindowsMediaStatusText` | — | Keine gesicherte Zuordnung |
| `WindowsMediaTitleText` | — | Keine gesicherte Zuordnung |
| `WindowsMediaArtistText` | — | Keine gesicherte Zuordnung |
| `WindowsMediaAlbumText` | — | Keine gesicherte Zuordnung |
| `WindowsMediaPlaybackText` | — | Keine gesicherte Zuordnung |
| `WindowsMediaSourceText` | — | Keine gesicherte Zuordnung |
| `WindowsMediaTimeText` | — | Keine gesicherte Zuordnung |
| `WinampStatusText` | — | Keine gesicherte Zuordnung |
| `WinampTitleText` | — | Keine gesicherte Zuordnung |
| `WinampArtistText` | — | Keine gesicherte Zuordnung |
| `WinampAlbumText` | — | Keine gesicherte Zuordnung |
| `WinampPlaybackText` | — | Keine gesicherte Zuordnung |
| `WinampTimeText` | — | Keine gesicherte Zuordnung |
| `WinampPlaylistText` | — | Keine gesicherte Zuordnung |
| `WinampAudioText` | — | Keine gesicherte Zuordnung |
| `WinampTelemetryStatusText` | — | Keine gesicherte Zuordnung |
| `WinampVuLeftBar` | — | Keine gesicherte Zuordnung |
| `WinampVuLeftText` | — | Keine gesicherte Zuordnung |
| `WinampVuRightBar` | — | Keine gesicherte Zuordnung |
| `WinampVuRightText` | — | Keine gesicherte Zuordnung |
| `WinampSpectrumText` | — | Keine gesicherte Zuordnung |
| `WinampConnectionDetailText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyStatusText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyTitleText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyArtistText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyAlbumText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyPlaybackText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyTimeText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyPlaylistText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyVisualizationStatusText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyVuLeftBar` | — | Keine gesicherte Zuordnung |
| `WmpLegacyVuLeftText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyVuRightBar` | — | Keine gesicherte Zuordnung |
| `WmpLegacyVuRightText` | — | Keine gesicherte Zuordnung |
| `WmpLegacySpectrumText` | — | Keine gesicherte Zuordnung |
| `WmpLegacyConnectionDetailText` | — | Keine gesicherte Zuordnung |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `CreateWindowsMediaPreset_Click` | `CreatePageFromPresetAsync` | Keine gesicherte 1:1-Implementierung |
| `CreateWinampPreset_Click` | `CreatePageFromPresetAsync` | Keine gesicherte 1:1-Implementierung |

## Events

WPF: Event-Overlay (Zeilen, Priorität, Dauer), Queue, Auswahl, Warn-Test, Clear, Cancel. Avalonia hat keine EventsView.

**WPF:** 7 benannte Controls, 5 XAML-Handler. **Keine eigenständige Avalonia-View.**

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `EventLine1TextBox` | — | Keine gesicherte Zuordnung |
| `EventLine2TextBox` | — | Keine gesicherte Zuordnung |
| `EventPriorityComboBox` | — | Keine gesicherte Zuordnung |
| `EventDurationTextBox` | — | Keine gesicherte Zuordnung |
| `EventQueueSummaryText` | — | Keine gesicherte Zuordnung |
| `EventQueueListBox` | — | Keine gesicherte Zuordnung |
| `SelectedEventText` | — | Keine gesicherte Zuordnung |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `AddEvent_Click` | `GetSelectedEventPriority`, `TryParse`, `InvalidOperationException`, `QueueDisplayEventAsync`, `NewGuid`, `FromSeconds` | Keine gesicherte 1:1-Implementierung |
| `TestWarningEvent_Click` | `QueueDisplayEventAsync`, `NewGuid`, `FromSeconds` | Keine gesicherte 1:1-Implementierung |
| `ClearEvents_Click` | `Clear`, `RenderRuntimePageAsync`, `RefreshEventsView` | Keine gesicherte 1:1-Implementierung |
| `EventQueueListBox_SelectionChanged` | `ToLocalTime` | Keine gesicherte 1:1-Implementierung |
| `CancelSelectedEvent_Click` | `Remove`, `RenderRuntimePageAsync`, `RefreshEventsView` | Keine gesicherte 1:1-Implementierung |

## Hardware

WPF: Sensor-Filter/Typ/Suche, Detail, Alias/Favorit, Fan-Zuweisung, Presets. Avalonia implementiert vieles; genaue Fehler-/Persistenzparität offen.

**WPF:** 7 benannte Controls, 6 XAML-Handler. Avalonia-View vorhanden, Parität offen.

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `HardwareSummaryText` | `HardwareSensorStatus` | Umbenanntes UI; Semantik offen |
| `HardwareSearchTextBox` | `HardwareSearch` | Umbenanntes UI; Semantik offen |
| `HardwareTypeComboBox` | `HardwareTypeFilter` | Umbenanntes UI; Semantik offen |
| `HardwareSensorsListBox` | `HardwareSensorList` | Umbenanntes UI; Semantik offen |
| `SelectedHardwareSensorText` | `HardwareSelectedSensor` | Umbenanntes UI; Semantik offen |
| `HardwareAliasTextBox` | `HardwareAlias` | Umbenanntes UI; Semantik offen |
| `HardwareFavoriteCheckBox` | `HardwareFavorite` | Umbenanntes UI; Semantik offen |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `HardwareFilterChanged` | `RefreshHardwareSensors` | `FilterHardwareSensors` – Codebeleg, Verhaltensgleichheit offen |
| `RefreshHardware_Click` | `RefreshHardwareSensors`, `RefreshFanSensorChoices` | `HardwareRefresh` – Codebeleg, Verhaltensgleichheit offen |
| `HardwareSensorsListBox_SelectionChanged` | Handler prüfen | `HardwareSensorList` – Codebeleg, Verhaltensgleichheit offen |
| `SaveHardwarePreference_Click` | `InvalidOperationException`, `FirstOrDefault`, `Equals`, `Trim`, `Add`, `Remove` | `HardwareSavePreference` – Codebeleg, Verhaltensgleichheit offen |
| `AssignHardwareSensorToFan_Click` | `InvalidOperationException`, `TryParse`, `SaveAsync`, `RefreshFanSensorChoices` | `AssignHardwareSensor` – Codebeleg, Verhaltensgleichheit offen |
| `CreateHardwarePreset_Click` | `CreatePageFromPresetAsync` | `HardwarePreset` – Codebeleg, Verhaltensgleichheit offen |

## Fan Control

WPF: automatische und manuelle Fan-Ausgabe, Kurve, Fail-safe, Sensor, Hysterese, Live-Status. Avalonia-Konfiguration/Simulation ≠ nachgewiesene physische Ausgabefunktion; Sicherheitsabweichung dokumentieren.

**WPF:** 18 benannte Controls, 6 XAML-Handler. Avalonia-View vorhanden, Parität offen.

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `AutomaticFanControlCheckBox` | `FanAutomatic` | Umbenanntes UI; Semantik offen |
| `Fan1Slider` | `DashboardFan1Slider` | Nur Dashboard-Element, nicht Fan-Tab |
| `Fan2Slider` | `DashboardFan2Slider` | Nur Dashboard-Element, nicht Fan-Tab |
| `Fan3Slider` | `DashboardFan3Slider` | Nur Dashboard-Element, nicht Fan-Tab |
| `Fan4Slider` | `DashboardFan4Slider` | Nur Dashboard-Element, nicht Fan-Tab |
| `FanChannelsListBox` | `FanChannelPicker` | Umbenanntes UI; Semantik offen |
| `FanNameTextBox` | `FanName` | Umbenanntes UI; Semantik offen |
| `FanModeComboBox` | `FanModePicker` | Umbenanntes UI; Semantik offen |
| `FanSensorComboBox` | `FanSensorKey` | Umbenanntes UI; Semantik offen |
| `FanFixedTextBox` | `FanFixed` | Umbenanntes UI; Semantik offen |
| `FanMinimumTextBox` | `FanMinimum` | Umbenanntes UI; Semantik offen |
| `FanMaximumTextBox` | `FanMaximum` | Umbenanntes UI; Semantik offen |
| `FanFailSafeTextBox` | `FanFailSafe` | Umbenanntes UI; Semantik offen |
| `FanCurveTextBox` | `FanCurve` | Umbenanntes UI; Semantik offen |
| `FanHysteresisTextBox` | `FanHysteresis` | Umbenanntes UI; Semantik offen |
| `FanAllowStopCheckBox` | `FanAllowStop` | Umbenanntes UI; Semantik offen |
| `FanLiveStatusText` | `FanLivePreview` | Umbenanntes UI; Semantik offen |
| `FanCurvePreviewCanvas` | `FanPreviewStatus` | Umbenanntes UI; Semantik offen |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `AutomaticFanControlChanged` | `Clear`, `SaveAsync`, `ApplyAutomaticFanControlAsync` | `FanAutomatic` – Codebeleg, Verhaltensgleichheit offen |
| `Fans_Click` | `ApplyManualFanValuesAsync`, `GetSliderPercent` | Keine gesicherte 1:1-Implementierung |
| `FanChannelsListBox_SelectionChanged` | `SelectFanMode`, `FormatFanCurve`, `RefreshFanCurvePreview`, `RefreshFanLiveStatus` | `FanChannelPicker` – Codebeleg, Verhaltensgleichheit offen |
| `RefreshFanSensors_Click` | `BeginInvoke`, `async`, `RefreshWinampView`, `RenderRuntimePageAsync` | `FanReadSensors` – Codebeleg, Verhaltensgleichheit offen |
| `FanCurveTextChanged` | `RefreshFanCurvePreview`, `ParseFanCurve`, `Clear` | `FanCurve` – Codebeleg, Verhaltensgleichheit offen |
| `SaveFanChannel_Click` | `InvalidOperationException`, `ParsePercent`, `TryParse`, `Show`, `GetSelectedFanMode`, `Format` | `FanSave` – Codebeleg, Verhaltensgleichheit offen |

## Settings

WPF: Virtual/Serial, Port, Theme, Sprache, Windows-Startup. Avalonia-Transport vorhanden; Theme/Sprache/Startup nicht nachgewiesen.

**WPF:** 5 benannte Controls, 6 XAML-Handler. Avalonia-View vorhanden, Parität offen.

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `TransportModeComboBox` | `SerialPortPicker` | Umbenanntes UI; Semantik offen |
| `PortComboBox` | `SerialPortPicker` | Umbenanntes UI; Semantik offen |
| `ThemeModeComboBox` | — | Keine gesicherte Zuordnung |
| `LanguageModeComboBox` | — | Keine gesicherte Zuordnung |
| `StartWithWindowsCheckBox` | — | Keine gesicherte Zuordnung |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `TransportModeChanged` | `UpdateTransportUi` | Keine gesicherte 1:1-Implementierung |
| `RefreshPorts_Click` | `Equals`, `InvalidOperationException`, `SaveAsync`, `ReconnectAsync`, `WriteFrameAsync` | Keine gesicherte 1:1-Implementierung |
| `ApplyDeviceSettings_Click` | `Equals`, `InvalidOperationException`, `SaveAsync`, `ReconnectAsync`, `WriteFrameAsync` | Keine gesicherte 1:1-Implementierung |
| `ThemeModeChanged` | `Apply`, `UpdateNavigationSelection`, `SaveAsync`, `default` | Keine gesicherte 1:1-Implementierung |
| `LanguageModeChanged` | `Apply`, `RefreshAppearanceChoices`, `ApplyTo`, `RefreshPagePresetChoices`, `RefreshLocalizedSectionHeader`, `RefreshWinampView` | Keine gesicherte 1:1-Implementierung |
| `StartWithWindowsChanged` | `SetEnabled`, `IsEnabled` | Keine gesicherte 1:1-Implementierung |

## Diagnostics

WPF: Snapshot, Virtual-State, Command History, Log und Export. Avalonia-Snapshot/Refresh vorhanden, übrige Ansichten/Export fehlen.

**WPF:** 4 benannte Controls, 2 XAML-Handler. Avalonia-View vorhanden, Parität offen.

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `DiagnosticsTextBox` | `DiagnosticsSnapshot` | Umbenanntes UI; Semantik offen |
| `VirtualLis2StateTextBox` | — | Keine gesicherte Zuordnung |
| `CommandHistoryTextBox` | — | Keine gesicherte Zuordnung |
| `LogTextBox` | — | Keine gesicherte Zuordnung |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `ExportDiagnostics_Click` | `RefreshDiagnostics`, `files`, `ShowDialog`, `Join`, `string`, `WriteAllText` | Keine gesicherte 1:1-Implementierung |
| `RefreshDiagnostics_Click` | `BuildDiagnosticsSnapshot`, `RefreshVirtualDiagnostics` | `DiagnosticsRefresh` – Codebeleg, Verhaltensgleichheit offen |

## Home Assistant

WPF: Enable, URL, Token, Test/Save, Entity-/Domain-Filter, Detail, Alias/Favorit, Attribute, Copy Keys, Seiten erzeugen. Avalonia hat HA-Test/Connect/Picker im Pages-Editor, aber keine eigene Verwaltungsansicht.

**WPF:** 14 benannte Controls, 11 XAML-Handler. **Keine eigenständige Avalonia-View.**

| WPF-Control | Tatsächlicher Avalonia-UI-Beleg | Bewertung |
|---|---|---|
| `HomeAssistantEnabledCheckBox` | — | Keine gesicherte Zuordnung |
| `HomeAssistantUrlTextBox` | `HaUrl` | Umbenanntes UI; Semantik offen |
| `HomeAssistantTokenPasswordBox` | `HaToken` | Umbenanntes UI; Semantik offen |
| `HomeAssistantSearchTextBox` | `HaEntityFilter` | Umbenanntes UI; Semantik offen |
| `HomeAssistantDomainComboBox` | — | Keine gesicherte Zuordnung |
| `HomeAssistantStatusText` | `HaStatus` | Umbenanntes UI; Semantik offen |
| `HomeAssistantEntitiesListBox` | `HaEntityPicker` | Umbenanntes UI; Semantik offen |
| `HomeAssistantDetailBorder` | — | Keine gesicherte Zuordnung |
| `SelectedHomeAssistantEntityText` | — | Keine gesicherte Zuordnung |
| `HomeAssistantAliasTextBox` | — | Keine gesicherte Zuordnung |
| `HomeAssistantFavoriteCheckBox` | — | Keine gesicherte Zuordnung |
| `HomeAssistantEntityActionsPanel` | — | Keine gesicherte Zuordnung |
| `HomeAssistantAttributeActionsPanel` | — | Keine gesicherte Zuordnung |
| `HomeAssistantAttributesListBox` | — | Keine gesicherte Zuordnung |

| WPF-Ereignishandler | WPF-Code: beobachtete Aufrufe | Avalonia-Anknüpfung |
|---|---|---|
| `TestHomeAssistant_Click` | `HomeAssistantDataSource`, `Configure`, `TestConnectionAsync`, `Show` | `HaTest` – Codebeleg, Verhaltensgleichheit offen |
| `SaveHomeAssistant_Click` | `Trim`, `TryCreate`, `not`, `InvalidOperationException`, `SaveAsync`, `ConfigureHomeAssistantSource` | `HaConnect` – Codebeleg, Verhaltensgleichheit offen |
| `HomeAssistantFilterChanged` | `RefreshHomeAssistantView` | `HaEntityFilter` – Codebeleg, Verhaltensgleichheit offen |
| `HomeAssistantEntitiesListBox_SelectionChanged` | `FirstOrDefault`, `Equals`, `NormalizeHomeAssistantAlias`, `OrderBy`, `Select`, `HomeAssistantAttributeRow` | `HaEntityPicker` – Codebeleg, Verhaltensgleichheit offen |
| `HomeAssistantEntitiesListBox_MouseDoubleClick` | Handler prüfen | Keine gesicherte 1:1-Implementierung |
| `CopyHomeAssistantEntityKey_Click` | `SetText` | Keine gesicherte 1:1-Implementierung |
| `SaveHomeAssistantEntity_Click` | `InvalidOperationException`, `FirstOrDefault`, `Equals`, `Trim`, `Add`, `Remove` | Keine gesicherte 1:1-Implementierung |
| `CreateHomeAssistantPage_Click` | `InvalidOperationException`, `NormalizeHomeAssistantAlias`, `NewGuid`, `Add`, `PersistPagesAsync`, `LoadPagesIntoRuntime` | Keine gesicherte 1:1-Implementierung |
| `CopyHomeAssistantAttributeKey_Click` | `SetText` | Keine gesicherte 1:1-Implementierung |
| `CreateHomeAssistantAttributePage_Click` | `InvalidOperationException`, `Trim`, `NewGuid`, `Add`, `PersistPagesAsync`, `LoadPagesIntoRuntime` | Keine gesicherte 1:1-Implementierung |
| `HomeAssistantAttributesListBox_MouseDoubleClick` | Handler prüfen | Keine gesicherte 1:1-Implementierung |

## Querschnittliche zwingende Prüfungen

- WPF-Codepfade außerhalb XAML: Timer, Event-Queue, Serial/Virtual-Verbindung, Medienquellen, Home-Assistant-Sync, Tray, Help/About, Presets, Startup und Fehlerbehandlung. Diese sind mit obiger XAML-Traceability **noch nicht vollständig semantisch auditiert**.
- Persistenz: WPF `AppSettings`/`SettingsStore` gegen Avalonia `PortableWorkspaceStore` und Import; Defaults, Migration, Geheimnisse, Fehlerrücknahme, Favoriten, Seiten, Lüfter.
- Darstellung: Texte, Tooltips, Tastatur, Enabled-State, Theme, Sprache, Fenster, Dialoge, WPF-Anordnung je Menü.
- Sicherheit: Physische Fan-Ausgabe bleibt deaktiviert bis zur sicherheitsgeprüften Portierung; WPF-Verhalten nicht blind übernehmen.
- Tests: automatisierte Windows-/Linux-Paritätstests und CI; keine Hardware verfügbar; manuelle Desktop-Endabnahme erst nach vollständiger Portierung.

**Offener Befund:** Die 126 benannten Controls und 71 pro Tab referenzierten Handler sind erfasst; ein vollständiger semantischer 1:1-Funktionsabgleich aller dynamischen und impliziten WPF-Codepfade ist damit **noch nicht abgeschlossen**. Fehlende Views: Media, Events, Home Assistant. Keine erfundenen Funktionen implementieren.

## WPF-authoritative tab ownership: Home Assistant versus Pages (2026-10-14)

Owner requirement: every Avalonia menu must contain exactly the functions and controls owned by its WPF TabItem, with equivalent behavior. No extra functions, no moving source configuration into the Pages editor.

WPF Home Assistant / Connection: HomeAssistantEnabledCheckBox; HomeAssistantUrlTextBox; HomeAssistantTokenPasswordBox; Test connection; Save / reconnect. WPF does NOT expose separate Connect HA and Disconnect HA actions here.

WPF Home Assistant / Entity browser: HomeAssistantSearchTextBox; HomeAssistantDomainComboBox; HomeAssistantStatusText; HomeAssistantEntitiesListBox with favorite, entity, domain, state, template key; HomeAssistantDetailBorder; SelectedHomeAssistantEntityText; HomeAssistantAliasTextBox; HomeAssistantFavoriteCheckBox; entity save/copy/create-page actions. HomeAssistantAttributesListBox with attribute copy/create-page actions. WPF double-click copies entity/attribute keys.

WPF Pages: presets, page CRUD/reorder, editor, visibility builder, overflow, scroll timing, live preview, save and event test. NO Home Assistant connection configuration. WPF Settings: transport, theme/language, startup; NO HA connection configuration.

Avalonia defect: HaUrl, HaToken, HaConnect, HaDisconnect, HaTest, HaStatus, HaEntityFilter, HaEntityPicker, HaInsertLine1, HaInsertLine2 are currently inside PagesView. HomeAssistantView does not exist. These are not HA-tab parity. Replace the misplaced inline setup with a complete WPF-equivalent HA tab; do not simply move controls and declare completion. Every other tab requires the same parent-tab ownership audit, defaults, labels, validation, persistence and handler semantics.

Status: verified placement requirements only; implementation NOT yet corrected. Safety restrictions and final deferred manual desktop testing remain in force.

## All-menu WPF placement and grouping audit — 2026-10-14

**Binding rule:** The WPF TabItem is the authority for *which menu owns each function*, and WPF cards, action labels, defaults and interactions define the target. A similarly named Avalonia helper does not establish UI parity. This section compares actual WPF MainWindow.xaml tab structure with Avalonia MainWindow.axaml; functional equivalence of all event handlers is still a separate acceptance gate.

| Menu | WPF-owned groups/actions | Avalonia placement finding | Required correction |
|---|---|---|---|
| Dashboard | VFD Preview; System Status; Quick Links; Fan Outputs (four fan sliders and Apply). | DashboardView exists, but fan output interaction is not WPF-equivalent; virtual preview/runtime behavior must be verified. | Preserve only WPF dashboard actions and card order; no invented connect buttons. |
| Display | Quick Display Test; Character ROM / Special Characters; Custom Characters / CG Builder; Brightness. | DisplayView contains the four corresponding sections. Detailed commands, glyph editing and defaults not fully proven. | Audit command behavior, character ROM range, glyph persistence, four brightness actions. |
| Pages | Preset and page list/CRUD/reorder; editor; condition, overflow and scrolling; live preview; Save page; Test event (5 s). | PagesView has inline Home Assistant connection, entity filtering and insert buttons, plus Windows settings import and playback buttons not present in WPF Pages. Some page runtime actions are relocated/extra. | Remove/move non-WPF sections; reproduce exact WPF Pages editor and its test-event behavior. |
| Media | Windows Media Player; Winamp; Windows Media Player Legacy; page variables, playback/VU/spectrum page actions. | No MediaView. Media navigation falls through to a generic not-connected description. | Implement WPF source-specific UI; distinguish Windows-only providers and Linux compatibility without inventing menu actions. |
| Events | Line 1/2, Priority, Duration, Show event, Test warning, Clear all, active/queued event list and Cancel selected. | No EventsView. | Implement WPF event creation, queue inspection and cancellation behavior. |
| Hardware | Hardware Monitoring; sensor search/filter; columns, selected sensor, alias/favorite, four fan assignments; display presets. | HardwareView exists and contains corresponding major sections; behavior and control placement not fully verified. | Retain WPF list/detail/preset structure and verify save/filter/assignment semantics. |
| Fan Control | Fan Outputs (Automatic control, four channels, Apply); Channels (Refresh sensors, editor, curve, hysteresis, stop, live state, save). | FanControlView uses four-channel configuration, Linux hwmon, virtual protocol and experimental authorization sections, not WPF's two-card layout. Physical fan output intentionally safety-gated. | Restructure into WPF Fan Outputs and Channels; preserve safety authorization invisibly or document necessary safety deviation, no extra feature claims. |
| Settings | Device / Transport (Virtual/Serial, COM port, Refresh, Apply/reconnect); Appearance (Theme, Language); Windows startup (tray). | SettingsView emphasizes transport and experimental serial controls; WPF Appearance/Windows startup not implemented in equivalent layout. | Restore WPF groupings and behavior; Windows-only startup handled as documented platform difference. |
| Diagnostics | Diagnostics Snapshot with Export report/Refresh; Virtual LIS2 state; Decoded command history; Protocol Log. | DiagnosticsView exists as Runtime diagnostics, but the four WPF panels and export/history/log controls are not all present. | Rebuild four WPF diagnostic panels with equivalent commands and output. |
| Home Assistant | Connection (enable, URL, token, Test connection, Save/reconnect); entity browser, domain/search, selection detail, attributes and copy/create-page actions. | No HomeAssistantView; HA connection and picker wrongly embedded in PagesView. | Remove inline HA configuration from Pages and implement full WPF Home Assistant view, keeping Pages only page-related functions. |

### Placement acceptance before further feature implementation

- Do not add actions or UI cards absent from WPF. Do not relocate connection settings into editors. In particular, HA configuration belongs to Home Assistant, Windows settings import is not a Pages-tab WPF action, and page playback buttons are not part of the WPF Pages control set.
- Do not remove implemented backend logic just because its Avalonia placement is wrong; relocate/reuse where WPF requires, and separately assess WPF-equivalent behaviors.
- Fan output safety remains an explicit exception to literal WPF physical behavior: keep hardware outputs disabled without authorization and test proof.
- The Media, Events and Home Assistant tabs must have dedicated views, not placeholders or a generic not-connected message.
- Validate WPF default values, control visibility, enabled state, user-visible wording, grouping, persistence, and navigation for each menu. No menu is approved by this source-level placement audit alone.

## Post-change placement recheck (2026-10-14)
- HA URL/token/test/save-connect and entity picker are now owned by HomeAssistantView rather than PagesView. This corrects the most obvious ownership violation, but **does not finish** WPF Home Assistant: enable checkbox is not yet wired; entity detail, alias, favorite, domain filter, attributes and WPF copy/create-page actions are absent. Pages still has extra import/playback controls. Other tab corrections remain open. Do not mark the overall requested correction pass complete.

## HA correction pass (2026-10-14)
- HomeAssistantView now wires domain filter, alias/favorite persistence, selected entity and attribute browsing, copy key and create display page actions. Extra insert-line buttons removed. Source-level comparison still flags WPF entity list presentation and attribute key semantics as needing automated parity tests; do not claim completion.

## Follow-up menu ownership check (2026-10-14)
- Pages no longer owns Windows settings import. Import UI relocated to Settings (platform migration action, not native WPF Settings feature); it remains a cross-platform migration exception pending owner decision. WPF labels now used for Fan Outputs, Automatic control, Save fan channel, Device / Transport, Diagnostics Snapshot, Refresh and Test event (5 s). Missing/extra actions and WPF semantics remain open.

## Diagnostics post-correction audit (2026-10-14)
- Four WPF diagnostics groups and Export report/Refresh actions now present. Snapshot and virtual state use actual available data. History and protocol log have honest unavailable labels pending real data source port, so diagnostics is structurally aligned but functionally incomplete.

## Diagnostics data parity pass (2026-10-14)
- Virtual LIS2 state and decoded command history now sourced from VirtualLis2Transport and formatted following WPF RefreshVirtualDiagnostics. Real protocol log remains outstanding. Diagnostics not yet full parity.

## User-prioritized virtual LIS2 and VFD color pass (2026-10-14)
- WPF VfdBackgroundBrush/VfdBorderBrush/VfdTextBrush are identical in Dark/Light (#123B8A/#5D8FE8/#F7FBFF). Avalonia Dashboard and Pages previews now use these exact colors. Hardware-free virtual LIS2 connects at window Opened to allow immediate display testing; serial transport remains explicit. UI smoke test still required.

## WPF automatic page rendering parity (2026-10-14)
- WPF startup calls RenderRuntimePageAsync and starts _pageTimer. Avalonia previously required explicit Start sequence. Avalonia now starts its existing PageSequenceController on successful virtual auto-connect; manual display actions still interrupt sequence. Linux runtime verification pending.
