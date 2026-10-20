# WPF-/Avalonia-Paritätsaudit

Stand: 20.10.2026. Implementierungsstand: PORT-03.

Referenz: [WPF-XAML](../../src/LIS2.App/MainWindow.xaml), [WPF-Code](../../src/LIS2.App/MainWindow.xaml.cs), [Avalonia-AXAML](../../src/LIS2.App.Avalonia/MainWindow.axaml) und zugehörige partielle Dateien. Vorhandene Ansichten oder identische Controlnamen sind keine Verhaltens- oder Darstellungsabnahme.

## Aktuelle Matrix

| Ansicht | Vorhandener Avalonia-Pfad | Offene Unterschiede und Prüfungen |
| --- | --- | --- |
| Dashboard | VFD-Vorschau und Status am angewendeten Transport; vier manuelle Fan-Regler mit WPF-Grenzen und AllowStop, serielle Freigabe je Aktion | Automatische Regelung sperrt Handausgabe. Physische Ausgabe und weiterer Live-/WPF-Abgleich offen |
| Display | Gemeinsame Zeilen-, Clear-, Sonderzeichen-/ROM-, Glyphen- und Helligkeitsaktionen über IDisplaySession; Helligkeit gespeichert und beim Verbinden angewendet | Headless-Controltests und Sichtprüfung vorhanden; physische Zeichen und native Glyphen-Dateidialoge offen |
| Pages | Liste, Presets, Dauer/Refresh, zwei Templates/Überlaufmodi, Sichtbarkeit, Scrolling, Vorschau, Dateifunktionen und Variablenbrowser; Quellenaktionen mit Entwurfsschutz/Reload, Wiederherstellung leerer Seitenlisten | Gemeinsamer Aktionsablauf headless getestet; GUI-Klicks nicht automatisiert. Zusätzliche Start/Stop-/Dateiaktionen gegen WPF-Umfang prüfen; genehmigten Variablenbrowser erhalten |
| Media | WindowsMedia-, Winamp-, WmpLegacy- und Linux-Karte; MainWindow.Media / WindowsMediaBridge / LinuxMprisSource; fehlende Anbieter sichtbar und im GUI-Smoke geprüft | WMP Legacy wird entgegen der zurückgestellten Produktentscheidung gestartet. Linux: playerctl-Metadaten, keine Position/Playerwahl; reale Player offen |
| Events | Eingabe, Priority/Lifetime, Queue, Test, Cancel/Clear; MainWindow.Events und gemeinsame Runtime-EventQueue | Prioritäts-/Ablaufmodelle headless getestet; Auswahl-, Fehlereingaben und WPF-gleiche GUI-Aktionen offen |
| Hardware | Gruppierte Liste, Suche/Typfilter, Details, Aliase/Favoriten, Fan-Zuordnung und Presets; MainWindow.Hardware | Reale Sensor-/Treiberidentität, Rechte, Auswahl-/Liveverhalten, Plattformschlüssel und exakte WPF-Bedienung prüfen |
| Fan Control | Vier Kanäle, Modi, Grenzen, Hysterese, Kurventext/-vorschau, Dry-run, virtuelle und serielle Regelung; MainWindow.Fans | Separate Test-/Freigabeaktionen gegenüber WPF abgleichen. Sicherheit erhalten; Tastatur, fehlerhafte Kurven, Status und Werte bei aktivem Runtimepfad prüfen |
| Settings | Transport/Port/HA-Aktivierung gespeichert, explizites Anwenden/Trennen, Helligkeit beim Connect, sichtbarer Windows-Import einschließlich Hardwarepräferenzen | Nativer Import-Dateidialog und Linux-Tray/Autostart offen; erweiterte Serial-Testcontrols bleiben ausgeblendet |
| Diagnostics | Snapshot, virtueller Zustand, dekodierte Historie, UTC-Anwendungs-/Transportjournal und Export; MainWindow.Diagnostics | Begrenztes Sitzungsjournal statt dauerhaftem Logarchiv. Native Dateiauswahl und Exportformat gegen WPF prüfen |
| Home Assistant | Eigene View mit Enable/URL/Token/Test/Save, Entitäten, Attributen, Copy/Create; Entwurfsschutz/Reload und eigenes persistiertes Aktivierungsfeld | Nicht in Pages eingebettet. Tokenstore-Ausweichpfad, echte Deaktivierung/Wiederverbindung und Linux-Liveprüfung offen |

Alle zehn Views sind im AXAML vorhanden und über die Navigation angebunden. „Media, Events und Home Assistant fehlen“ beschreibt diesen Code nicht.

## Hilfe, About und Querschnitt

Hilfe und About sind vorhanden: `OpenHelp`/`OpenAbout` in der Sidebar, Dialoge in `MainWindow.HelpAbout.cs`, zehn Hilfethemen in `HelpReferenceCatalog.cs`. Die Kontextauswahl folgt der aktuellen Hauptansicht. Die WPF-Hilfe befindet sich dagegen am Abschnittskopf; diese Platzierung ist nicht gleich.

Die Hilfeabsätze sind überwiegend Englisch und gehen durch Übersetzungshelfer. Übersetzungshelfer allein belegen keine passenden Übersetzungen. Inhaltstiefe und Verhalten mit langen Variablenlisten benötigen Abgleich. About enthält Version, Build, Runtime, Architektur, OS, tatsächliches Workspace-Verzeichnis und GitHub-Aktion. Beide Dialoge werden im GUI-Smoke geöffnet, gerendert und geschlossen; der externe GitHub-Link wird dabei nicht geöffnet.

System/Light/Dark, fünf Sprachkataloge, Windows-Tray und Autostart existieren. Traytexte sind fest Englisch; Linux hat keine entsprechende Anbindung. Theme-/DPI-/Fokus-/Tastatur-/Fenstergrenzen und Titelzusätze sind noch nicht durch Sichtprüfung nachgewiesen.

## Genehmigte Abweichung und Sicherheitsgrenzen

Der Benutzer hat den modalen Variablenbrowser als Ersatz der großen WPF-Variablen-ComboBox genehmigt. Er enthält Suche/Kategorien/HA-Filter, aktuelle Werte, persistente Favoriten und Einfügen in beide Zeilen. Stored Templates bleiben unverändert.

Physische Lüfterausgabe erfordert eine ausdrückliche, nicht persistierte Freigabe. Eine WPF-ähnliche Schaltfläche rechtfertigt kein Umgehen dieses Pfades. Zusätzliche/verschobene Bedienelemente, WMP-Legacy-Aktivierung und lokaler Tokenstore werden durch ihre Erwähnung nicht genehmigt.

## Prüfnachweise und verbleibende Abnahme

Lokal sind 332/332 Avalonia-Tests auf Windows erfolgreich. `DeviceUiTests` prüft Gerätebedienung, Einstellungen/Import und manuelle Lüfter mit tatsächlichen Controls, Tastatur und isolierten Stores. `RuntimeHardeningTests` prüft zusätzlich zehn Ansichten, Hilfe/About, fehlende Medienanbieter, Diagnose, verzögerten Start gegen Shutdown und differenzielle Ausgabe einschließlich Teilfehlern. Native Windows-Fenster werden über `--desktop-smoke` gestartet und geordnet beendet; CI enthält denselben Windows-/Linux-X11-Pfad. Sichtprüfungen, aktuelle CI-Läufe und gezielte Gegenproben sind im [Status](linux-port-status.md) eingegrenzt. Ein automatisierter Screenshotvergleich besteht nicht.

Für jede Ansicht bleiben die semantischen Fehler-/Leer-/Auswahl-/Speicherpfade und die Darstellung auf Windows/Linux zu prüfen. Manuelle Benutzerabnahme erfolgt nach dem vereinbarten Portierungsumfang; physische LIS2-Prüfung bleibt getrennt. WPF wird bis zur Paritätsabnahme erhalten. Prioritäten stehen in [TODO](../../TODO.md).

## Quellengeprüftes WPF-Inventar

Das folgende Inventar bewahrt die benannten Tab-Controls und direkt im XAML referenzierten Handler der Referenz. Jeder Controlname und Handler wurde beim Dokumentationsabgleich gegen das aktuelle WPF-XAML geprüft. Tabbezogene Mehrfachverwendungen werden einzeln gezählt. Unbenannte Controls, implizite Bindings, Hintergrundstarts und interne Aufrufe sind zusätzlich semantisch zu prüfen.

### Dashboard

7 benannte Controls, 2 XAML-Handlerverwendungen.

Controls: `PreviewLine1`, `PreviewLine2`, `VirtualStateText`, `DashboardFan1Slider`, `DashboardFan2Slider`, `DashboardFan3Slider`, `DashboardFan4Slider`.

Handler: `Navigation_Click`, `DashboardFans_Click`.

### Display

11 benannte Controls, 19 XAML-Handlerverwendungen.

Controls: `Line1TextBox`, `Line2TextBox`, `CharacterRomStartTextBox`, `CharacterRomRangeText`, `ProgramCustomGlyphsOnConnectCheckBox`, `CustomGlyphSlotComboBox`, `CustomGlyphNameTextBox`, `CustomGlyphTemplateKeyText`, `CustomGlyphRowsTextBox`, `CustomGlyphPreview`, `CustomGlyphLinePreview`.

Handler: `SendLine1_Click`, `SendLine2_Click`, `RenderBoth_Click`, `Clear_Click`, `SendSpecialCharacterTest_Click`, `CharacterRomPrevious_Click`, `SendCharacterRomRange_Click`, `CharacterRomNext_Click`, `ProgramCustomGlyphsOnConnectChanged`, `CustomGlyphSlotChanged`, `CustomGlyphNameChanged`, `CustomGlyphRowsChanged`, `SaveCustomGlyph_Click`, `SendCustomGlyph_Click`, `SendAllCustomGlyphs_Click`, `LoadCustomGlyphSet_Click`, `SaveCustomGlyphSet_Click`, `LoadExampleGlyphSet_Click`, `Brightness_Click`.

### Pages

17 benannte Controls, 12 XAML-Handlerverwendungen.

Controls: `PagePresetComboBox`, `PagesListBox`, `PageNameTextBox`, `PageLine1TextBox`, `PageLine2TextBox`, `PageDurationTextBox`, `PageRefreshIntervalTextBox`, `PageVisibilityKeyComboBox`, `PageVisibilityOperatorComboBox`, `PageVisibilityValueTextBox`, `PageVisibilityTextBox`, `PageLine1OverflowComboBox`, `PageLine2OverflowComboBox`, `PageScrollSpeedTextBox`, `PageEdgePauseTextBox`, `PageEditorPreviewLine1`, `PageEditorPreviewLine2`.

Handler: `CreatePagePreset_Click`, `PagesListBox_SelectionChanged`, `PageEnabledChanged`, `AddPage_Click`, `DuplicatePage_Click`, `DeletePage_Click`, `MovePageUp_Click`, `MovePageDown_Click`, `PageEditorChanged`, `BuildVisibilityExpression_Click`, `SavePage_Click`, `TestEvent_Click`.

### Media

36 benannte Controls, 2 XAML-Handlerverwendungen.

Controls: `WindowsMediaStatusText`, `WindowsMediaTitleText`, `WindowsMediaArtistText`, `WindowsMediaAlbumText`, `WindowsMediaPlaybackText`, `WindowsMediaSourceText`, `WindowsMediaTimeText`, `WinampStatusText`, `WinampTitleText`, `WinampArtistText`, `WinampAlbumText`, `WinampPlaybackText`, `WinampTimeText`, `WinampPlaylistText`, `WinampAudioText`, `WinampTelemetryStatusText`, `WinampVuLeftBar`, `WinampVuLeftText`, `WinampVuRightBar`, `WinampVuRightText`, `WinampSpectrumText`, `WinampConnectionDetailText`, `WmpLegacyStatusText`, `WmpLegacyTitleText`, `WmpLegacyArtistText`, `WmpLegacyAlbumText`, `WmpLegacyPlaybackText`, `WmpLegacyTimeText`, `WmpLegacyPlaylistText`, `WmpLegacyVisualizationStatusText`, `WmpLegacyVuLeftBar`, `WmpLegacyVuLeftText`, `WmpLegacyVuRightBar`, `WmpLegacyVuRightText`, `WmpLegacySpectrumText`, `WmpLegacyConnectionDetailText`.

Handler: `CreateWindowsMediaPreset_Click`, `CreateWinampPreset_Click`.

### Events

7 benannte Controls, 5 XAML-Handlerverwendungen.

Controls: `EventLine1TextBox`, `EventLine2TextBox`, `EventPriorityComboBox`, `EventDurationTextBox`, `EventQueueSummaryText`, `EventQueueListBox`, `SelectedEventText`.

Handler: `AddEvent_Click`, `TestWarningEvent_Click`, `ClearEvents_Click`, `EventQueueListBox_SelectionChanged`, `CancelSelectedEvent_Click`.

### Hardware

7 benannte Controls, 6 XAML-Handlerverwendungen.

Controls: `HardwareSummaryText`, `HardwareSearchTextBox`, `HardwareTypeComboBox`, `HardwareSensorsListBox`, `SelectedHardwareSensorText`, `HardwareAliasTextBox`, `HardwareFavoriteCheckBox`.

Handler: `HardwareFilterChanged`, `RefreshHardware_Click`, `HardwareSensorsListBox_SelectionChanged`, `SaveHardwarePreference_Click`, `AssignHardwareSensorToFan_Click`, `CreateHardwarePreset_Click`.

### Fan Control

18 benannte Controls, 6 XAML-Handlerverwendungen.

Controls: `AutomaticFanControlCheckBox`, `Fan1Slider`, `Fan2Slider`, `Fan3Slider`, `Fan4Slider`, `FanChannelsListBox`, `FanNameTextBox`, `FanModeComboBox`, `FanSensorComboBox`, `FanFixedTextBox`, `FanMinimumTextBox`, `FanMaximumTextBox`, `FanFailSafeTextBox`, `FanCurveTextBox`, `FanHysteresisTextBox`, `FanAllowStopCheckBox`, `FanLiveStatusText`, `FanCurvePreviewCanvas`.

Handler: `AutomaticFanControlChanged`, `Fans_Click`, `FanChannelsListBox_SelectionChanged`, `RefreshFanSensors_Click`, `FanCurveTextChanged`, `SaveFanChannel_Click`.

### Settings

5 benannte Controls, 6 XAML-Handlerverwendungen.

Controls: `TransportModeComboBox`, `PortComboBox`, `ThemeModeComboBox`, `LanguageModeComboBox`, `StartWithWindowsCheckBox`.

Handler: `TransportModeChanged`, `RefreshPorts_Click`, `ApplyDeviceSettings_Click`, `ThemeModeChanged`, `LanguageModeChanged`, `StartWithWindowsChanged`.

### Diagnostics

4 benannte Controls, 2 XAML-Handlerverwendungen.

Controls: `DiagnosticsTextBox`, `VirtualLis2StateTextBox`, `CommandHistoryTextBox`, `LogTextBox`.

Handler: `ExportDiagnostics_Click`, `RefreshDiagnostics_Click`.

### Home Assistant

14 benannte Controls, 11 XAML-Handlerverwendungen.

Controls: `HomeAssistantEnabledCheckBox`, `HomeAssistantUrlTextBox`, `HomeAssistantTokenPasswordBox`, `HomeAssistantSearchTextBox`, `HomeAssistantDomainComboBox`, `HomeAssistantStatusText`, `HomeAssistantEntitiesListBox`, `HomeAssistantDetailBorder`, `SelectedHomeAssistantEntityText`, `HomeAssistantAliasTextBox`, `HomeAssistantFavoriteCheckBox`, `HomeAssistantEntityActionsPanel`, `HomeAssistantAttributeActionsPanel`, `HomeAssistantAttributesListBox`.

Handler: `TestHomeAssistant_Click`, `SaveHomeAssistant_Click`, `HomeAssistantFilterChanged`, `HomeAssistantEntitiesListBox_SelectionChanged`, `HomeAssistantEntitiesListBox_MouseDoubleClick`, `CopyHomeAssistantEntityKey_Click`, `SaveHomeAssistantEntity_Click`, `CreateHomeAssistantPage_Click`, `CopyHomeAssistantAttributeKey_Click`, `CreateHomeAssistantAttributePage_Click`, `HomeAssistantAttributesListBox_MouseDoubleClick`.
