# WPF-/Avalonia-Paritätsaudit

Stand: 27.10.2026. Implementierungsstand: PORT-08 und Windows Media Player Legacy.

Die Formatierungspipeline erhält Leerzeichen in Textargumenten und unterstützt zusätzlich `fallbackvar` für Ersatzvariablen. Leere, unknown und unavailable Werte wechseln zur nächsten Ersatzvariable, zuletzt zum festen Ersatztext. Gewählte Variablenwerte werden formatiert, fester Ersatztext bleibt unverändert. Die direkten Hinweise an beiden Zeilenfeldern sind lokalisiert verfügbar, einschließlich F1/Escape. Der einklappbare Formatierungseditor erzeugt Vorlagenausdrücke mit Liveergebnis, eigener Ersatzvariablenauswahl und Einfügen an der Schreibmarke. Die Seitenhilfe beschreibt Bedienung und Beispiele. Zahlen folgen dem Betriebssystem-Zahlenformat, unabhängig von der App-Sprache. Das ersetzt nicht die offene ROAD-03-Prüfung frei eingegebener Vorlagen.

PORT-06 ergänzt den softwareseitigen Abgleich um benannte Glyphenschlüssel, statische Variablenbeschreibungen ohne verbundene Quellen, Hilfe neben dem Bereichstitel und F1. `DesktopParityUiTests` prüft zehn Ansichten in fünf Sprachen mit drei Themes bei 950 × 600, Headergrenzen, Fokus, Kontexthilfe sowie gültige und ungültige Ereignis-/Lüftereingaben. Bestehende Geräte-, Seiten-, Medien- und HA-Tests bleiben Teil der Suite. Das ersetzt keine Abnahme nativer Dateidialoge, echter Quellen oder physischer Geräte.

Windows und Linux erhalten Tray-Menüs; Linux-XDG-Autostart, ein exklusiver Instanzhandle mit benutzerbeschränkter Aktivierung und Splash sind angebunden. Beim Wiederanzeigen bleiben Transport und Runtime bestehen. Plattformabweichung: Linux beendet beim Schließen und minimiert in die Taskleiste, Windows schließt/minimiert in den Infobereich. Native Tray-Klicks, Anmeldung und Benutzerabnahme bleiben offen.

Referenz: [WPF-XAML](https://github.com/Eitschman/lis2-control-center/blob/922ca1b6fb7a1edf9f8c344c71c7a63686405b8a/src/LIS2.App/MainWindow.xaml), [WPF-Code](https://github.com/Eitschman/lis2-control-center/blob/922ca1b6fb7a1edf9f8c344c71c7a63686405b8a/src/LIS2.App/MainWindow.xaml.cs), [Avalonia-AXAML](../../src/LIS2.App.Avalonia/MainWindow.axaml) und zugehörige partielle Dateien. Vorhandene Ansichten oder identische Controlnamen sind keine Verhaltens- oder Darstellungsabnahme.

## Direkter main-Abgleich und Paket PORT-06A

WPF-Funktionsreferenz des direkten Vergleichs: Git-Referenz, Commit `922ca1b6fb7a1edf9f8c344c71c7a63686405b8a`. Die folgenden Punkte beschreiben den umgesetzten Avalonia-Stand und seine Nachweisgrenzen. Die WPF-Projekte sind auf Benutzerauftrag entfernt; ihre Referenz ist über den genannten Git-Commit zugänglich.

| ID | Aktueller Avalonia-Pfad | Nachweis und Grenze |
| --- | --- | --- |
| A01 | `PageSourceValues.Snapshot` erzeugt `Winamp.SpectrumGlyphs` aus Rohdaten und aktivem Zeichensatz; andere Sätze verwenden das Textspektrum. | Beide Pegelbereiche 0..15/0..255, Preset und virtueller Gerätetransport geprüft; echter Winamp bleibt Zielsystemprüfung. |
| A02 | `GlyphRendering.Preview` übersetzt private VFD-Zeichen in Dashboard und Seitenvorschau zu Slotmarken oder Balken. | Normale Zeichen und Endstufen geprüft; Gerätezeichenpfad bleibt separat. |
| A03 | `LoadConditionEditor` synchronisiert Rohtext und Builder; der modale Variablenbrowser übernimmt einen Schlüssel. | Gültig, leer, ungültig und Browserauswahl mit tatsächlichen Controls geprüft. |
| A04 | `RefreshFanSensorChoices` verwendet den numerischen Sensorkatalog; freies Schlüsselfeld bleibt bestehen. | Direkte Auswahl und gespeicherter fehlender Schlüssel geprüft; physische Sensoren offen. |
| A05 | Stereo-VU, zwanzig Spektrumbänder, Pipe und Snapshotzeit; Windows-Quelle und Playback sichtbar. | Grafiken und Leeren bei Trennung geprüft und angesehen; Liveplayer offen. |
| A06 | Diagnose enthält Version, UTC, Transport/Port, gesendete Zeilen, Quellenfehler und vier Kanäle; `FailureLog` sichert Start-/Absturzspuren. | Tokens im Bericht geschwärzt; Fehlerdatei speichert Typ/Stack statt Nachrichten. Gültiger und blockierter Schreibpfad geprüft. |
| A07 | Geladene Sätze werden nach Speichern bei aktivierter Option an den verbundenen angewendeten Transport programmiert. | Virtuelle Tests mit Option an/aus sowie Speicherfehler; physische Übernahme offen. |
| A08 | Beide Hardware-Preset-Einstiege verwenden `HardwareDisplayPresetBuilder` und denselben Snapshot. | CPU, GPU, Memory, Temperatures und Fans mit identischer Fixture, Favorit und Einheiten verglichen. |
| A09 | Windows-Presets nur unter Windows, MPRIS unter Linux; Quellenbeschriftung folgt Plattform. | Windows-Controltest; native Linux-Ausführung für diesen Stand noch offen. |
| A10 | HA-Listen kopieren per Doppelklick und Strg+C; Kopierschaltflächen bleiben vorhanden. | Attributkopie mit tatsächlicher Maus und Tastatur gegen lokalen HA-Protokollpartner geprüft. |
| A11 | Gemeinsamer versionierter Speicherstatus an Seiten-, Glyphen-, Sensor- und HA-Editoren. | Seiten-/Glyphenentwürfe bleiben bei blockierter Schreibdatei erhalten; erneuter Schreibversuch geprüft. |
| A12 | Normale Lüfterkonfiguration vor aufklappbaren Prüfwerkzeugen; physische Freigabe separat. | Controls, Lokalisierung und Mindestgröße geprüft; Hardwarefreigabe bleibt nicht persistiert. |
| A13 | 5×8-Toggle-Raster und einzelne Kurvenpunkte verwenden vorhandene Textformate. | Pixel mit Leertaste; Dezimalpräzision, ungültige Eingaben und Punktänderung geprüft. Die neuen Editoren sind beauftragte Erweiterungen, keine fehlenden WPF-Funktionen. |
| A14 | Funktions- und Fehlerpfade, fünf Sprachen, Skia-Sichtprüfung und aktualisierte Hilfe. | 451 Avalonia-Tests erfolgreich, ein Linux-Fall übersprungen; weitere Nachweise und Grenzen im [Status](linux-port-status.md). |

Benutzernachweis: Windows-Start und Tray wurden im Chat am 21.10.2026 positiv bestätigt. Die genaue Artefaktherkunft sowie Einzelaktionen Öffnen/Beenden/Anmeldung sind damit nicht dokumentiert; ein extrahierter Paketstart oder Autostart bei Anmeldung wird daraus nicht abgeleitet.

## Laufzeit- und Speicherparität: PORT-06B

| ID | Aktueller Avalonia-Pfad | Nachweis und Grenze |
| --- | --- | --- |
| B01 | Gespeicherte Lüfterkanäle werden an laufende serielle/virtuelle Regler und den Dry-run übergeben; der nächste Regeltakt verwendet sie. | Festwert, geänderte Grenzen, Sensorwechsel und Fail-safe mit virtuellen Protokollpartnern geprüft. Speichern aktiviert keine Ausgabe; reale Lüfter bleiben ungeprüft. |
| B02 | Helligkeit und Glyphenprogrammierung verwenden das Frame-Gate ohne Neustart der Sequenz. | Beide Transportpfade erhalten aktuelle Seite, Timer und Overlay. Gestoppte Sequenzen bleiben gestoppt; laufender Frame und Fehlerfreigabe des Gates geprüft. |
| B03 | Seitenimport meldet erst nach erfolgreicher Workspace-Speicherung Erfolg. | Gültige Datei sowie blockierte Schreibdatei geprüft; Fehlermeldung bleibt sichtbar und die vorhandene Workspace-Datei erhalten. Native Dateiauswahl bleibt offen. |
| B04 | Bedingungsanzeige wird beim Vorschautakt und Sprachwechsel neu bewertet. | Wechsel von nicht erfüllt zu erfüllt mit aktuellen Sensordaten und Sprachwechsel geprüft; Skia-Sichtprüfung bei 950 × 600. |

## Aktuelle Matrix

| Ansicht | Vorhandener Avalonia-Pfad | Offene Unterschiede und Prüfungen |
| --- | --- | --- |
| Dashboard | VFD-Vorschau und Status am angewendeten Transport; vier manuelle Fan-Regler mit WPF-Grenzen und AllowStop, serielle Freigabe je Aktion | Automatische Regelung sperrt Handausgabe. Physische Ausgabe und weiterer Live-/WPF-Abgleich offen |
| Display | Gemeinsame Zeilen-, Clear-, Sonderzeichen-/ROM-, Glyphen- und Helligkeitsaktionen über IDisplaySession; Helligkeit gespeichert und beim Verbinden angewendet | Headless-Controltests und Sichtprüfung vorhanden; physische Zeichen und native Glyphen-Dateidialoge offen |
| Pages | Liste, Presets, Dauer/Refresh, zwei Templates/Überlaufmodi, Sichtbarkeit, Scrolling, Vorschau, Dateifunktionen und Variablenbrowser; Quellenaktionen mit Entwurfsschutz/Reload, Wiederherstellung leerer Seitenlisten | Spektrumvariable, Glyphenvorschau, Bedingungsfelder und gemeinsame Presets geprüft; physische Darstellung und native Dateibedienung offen |
| Media | WindowsMedia-/Winamp-Karten unter Windows, Linux-Karte mit gespeicherter Playerauswahl und Spielposition; Legacy-Karte mit getrenntem Audio-/Metadatenstatus und drei Presets; GSMTC-Paketstart ohne Player lokal geprüft | Grafische Telemetrie und Verbindungs-/Quellendetails vorhanden. Reale VLC-/Plexamp-/Windows-Media-Player-Sitzungen offen; keine eigene D-Bus-Subscription |
| Events | Eingabe, Priority/Lifetime, Queue, Test, Cancel/Clear; MainWindow.Events und gemeinsame Runtime-EventQueue | Priorität/Ablauf sowie gültige/ungültige Dauer, Auswahl und Abbruch mit Controls geprüft; reale Displayüberlagerung offen |
| Hardware | Gruppierte Liste, Suche/Typfilter, Details, Aliase/Favoriten, Fan-Zuordnung und Presets; MainWindow.Hardware | Reale Sensor-/Treiberidentität, Rechte, Auswahl-/Liveverhalten, Plattformschlüssel und exakte WPF-Bedienung prüfen |
| Fan Control | Vier Kanäle, Modi, Grenzen, Hysterese, Kurventext/-vorschau, Dry-run, virtuelle und serielle Regelung; MainWindow.Fans | Separate Test-/Freigabeaktionen gegenüber WPF abgleichen. Sicherheit erhalten; Tastatur, fehlerhafte Kurven, Status und Werte bei aktivem Runtimepfad prüfen |
| Settings | Transport/Port/HA-Aktivierung gespeichert, explizites Anwenden/Trennen, Helligkeit beim Connect, sichtbarer Windows-Import einschließlich Hardwarepräferenzen | Nativer Import-Dateidialog, Tray-Menübedienung und Anmeldung mit Autostart offen; erweiterte Serial-Testcontrols bleiben ausgeblendet |
| Diagnostics | Version, UTC, angewendeter Transport/Port, Sendeframe, Quellen-/Lüfterdetails, virtueller Zustand, Journal und Export; dauerhafte Start-/Absturzspuren | Begrenztes Sitzungsjournal; native Dateiauswahl und echte Hardwarediagnose prüfen |
| Home Assistant | Eigene View mit Enable/URL/Token/Test/Save, Entitäten, Attributnamen/-werten, Copy/Create; wirksame Aliase, Favoriten und persistierte Aktivierung; lokale Linux-AES-Wahl ausdrücklich genehmigt | Nicht in Pages eingebettet. Reale HA-Instanz und Desktop-Keyring auf Zielsystemen offen; Reconnect/Stop und Persistenz mit isolierten Daten geprüft |

Alle zehn Views sind im AXAML vorhanden und über die Navigation angebunden. „Media, Events und Home Assistant fehlen“ beschreibt diesen Code nicht.

## Hilfe, About und Querschnitt

Hilfe und About sind vorhanden: `OpenHelp` neben dem Bereichstitel und `OpenAbout` in der Sidebar, Dialoge in `MainWindow.HelpAbout.cs`, zehn Hilfethemen in `HelpReferenceCatalog.cs`. Die Kontextauswahl folgt der aktuellen Hauptansicht. F1 öffnet dieselbe kontextbezogene Hilfe.

Die zehn Hilfethemen verwenden einen eigenen Katalog für Englisch, Deutsch, Französisch, Türkisch und Russisch. Titel, Navigation, Schließen und Absätze folgen der gespeicherten Sprache oder dem Systemstandard, auch bei geöffnetem Dialog. Vorlagenschlüssel bleiben unverändert. Inhalte sind gegen die aktuellen Avalonia-Abläufe geprüft: fünf Lüftermodi, Sicherheitsgrenzen, Medien je Plattform, Formatierung, Sichtbarkeit und begrenzte Variablenlisten. Numerische und normalisierte benannte Glyphenschlüssel sind angebunden. Statische Clock-/Winamp-/WindowsMedia-/MPRIS-Schlüssel sind auch ohne verbundene Quelle mit Beschreibungen verfügbar; WPF-Fachübersetzungen werden gemeinsam verwendet. About enthält Version, Build, Runtime, Architektur, OS, tatsächliches Workspace-Verzeichnis und GitHub-Aktion; seine Beschriftungen folgen der ausgewählten Sprache und werden in den Sprach-UI-Tests geprüft. Beide Dialoge werden im GUI-Smoke geöffnet, gerendert und geschlossen; der externe GitHub-Link wird dabei nicht geöffnet.

System/Light/Dark, fünf Sprachkataloge, Windows-/Linux-Tray und plattformeigener Autostart existieren. Traytexte folgen dem gewählten Sprachkatalog; System-Themeänderungen aktualisieren die Farbressourcen. Alle zehn Menüseiten verwenden ergänzte Übersetzungen für Beschriftungen und dynamische Meldungen. Metadaten, Displayinhalte und gebundene Nutzdaten werden nicht übersetzt. Die schmale Ansicht verwendet umbrechende Aktionszeilen und einen gestapelten Seiteneditor. Die geprüften Fenstergrößen und verbleibenden Theme-/DPI-/Fokus-/Tastaturgrenzen stehen im Status.

## Genehmigte Abweichung und Sicherheitsgrenzen

Der Benutzer hat den modalen Variablenbrowser als Ersatz der großen WPF-Variablen-ComboBox genehmigt. Er enthält Suche/Kategorien/HA-Filter, aktuelle Werte, persistente Favoriten und Einfügen in beide Zeilen. Stored Templates bleiben unverändert.

Physische Lüfterausgabe erfordert eine ausdrückliche, nicht persistierte Freigabe. Eine WPF-ähnliche Schaltfläche rechtfertigt kein Umgehen dieses Pfades. Zusätzliche/verschobene Bedienelemente benötigen eine fachliche Grundlage. Die lokale Linux-Tokenspeicherung ist nur mit ausdrücklicher Auswahl in der App genehmigt; der automatische Ausweichpfad entfällt. WMP Legacy liefert Metadaten über das Hintergrund-Plug-in und Audio direkt über WASAPI aus dem Playerprozess. Plug-ins und Audioerfassung bleiben vom Hardwarezugriff getrennt. Reale MP3-Titelwechsel und minimierte Wiedergabe sind unter Windows 11 mit dem x86-Player geprüft; weitere Zielsysteme bleiben offen.

## Prüfnachweise und verbleibende Abnahme

Lokal sind 451 Avalonia-Tests auf Windows erfolgreich; ein Linux-Schlüsselbundtest ist übersprungen. `Port06BUiTests` und `Port06BRuntimeTests` prüfen zwölf Fälle zur laufenden Lüfterkonfiguration, exklusiven Gerätebedienung, Rotation, Importfehlern und Bedingungsanzeige. `DeviceUiTests` prüft Gerätebedienung, Einstellungen/Import und manuelle Lüfter mit tatsächlichen Controls, Tastatur und isolierten Stores. `MenuLanguageUiTests` prüft zehn Menüseiten, About, gespeicherte Sprache, Systemstandard, dynamische Meldungen, unveränderte Daten und stabile Lüftermodi. Katalogtests prüfen Textabdeckung und Formatplatzhalter. `HomeAssistantUiTests` prüft Verbindung, Alias/Favorit, Attributseite, Speicherfehler und Deaktivierung mit tatsächlichen Controls und lokalem WebSocket-Protokollpartner. `HelpLanguageUiTests` prüft gespeicherte Hilfesprache, Systemstandard, Sprachwechsel im offenen Dialog und die Navigation durch alle Themen; lokalisierte Ansichten werden bei 840 × 600 gerendert. `HelpReferenceCatalogTests` prüft Katalogabdeckung, unveränderte Schlüssel und ausführbare Beispiele. `RuntimeHardeningTests` prüft zusätzlich zehn Ansichten, Hilfe/About, fehlende Medienanbieter, Diagnose, verzögerten Start gegen Shutdown und differenzielle Ausgabe einschließlich Teilfehlern. Native Windows-Fenster werden über `--desktop-smoke` gestartet und geordnet beendet; CI enthält denselben Windows-/Linux-X11-Pfad. Sichtprüfungen, aktuelle CI-Läufe und gezielte Gegenproben sind im [Status](linux-port-status.md) eingegrenzt. Ein automatisierter Screenshotvergleich besteht nicht.

Für jede Ansicht bleiben die semantischen Fehler-/Leer-/Auswahl-/Speicherpfade und die Darstellung auf Windows/Linux zu prüfen. Manuelle Benutzerabnahme erfolgt nach dem vereinbarten Portierungsumfang; physische LIS2-Prüfung bleibt getrennt. Die WPF-Referenz ist über Git-Commit `922ca1b` zugänglich; offene manuelle Prüfungen bleiben bestehen. Prioritäten stehen in [TODO](../../TODO.md).

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
