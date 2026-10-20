using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    // Keep the original UI strings, so language changes are reversible without reloading a window.
    private readonly Dictionary<TextBlock, string> _originalPageLabels = new();

    private static readonly Dictionary<string, string> GermanPageLabels = new(StringComparer.Ordinal)
    {
        ["VFD Preview"] = "VFD-Vorschau",
        ["Preset"] = "Vorlage",
        ["System Status"] = "Systemstatus",
        ["Quick Links"] = "Schnellzugriff",
        ["Fan Outputs"] = "Lüfterausgänge",
        ["Display controls"] = "Displaysteuerung",
        ["Line 1"] = "Zeile 1",
        ["Line 2"] = "Zeile 2",
        ["Character ROM / Special Characters"] = "Zeichen-ROM / Sonderzeichen",
        ["ROM start byte"] = "ROM-Startbyte",
        ["Custom Characters / CG Builder"] = "Eigene Zeichen / CG-Editor",
        ["Slot"] = "Speicherplatz",
        ["Name"] = "Name",
        ["Template key"] = "Vorlagenschlüssel",
        ["Brightness"] = "Helligkeit",
        ["Display pages"] = "Displayseiten",
        ["Page properties"] = "Seiteneigenschaften",
        ["Duration (s)"] = "Dauer (s)",
        ["Refresh (ms)"] = "Aktualisierung (ms)",
        ["Visible when"] = "Sichtbar wenn",
        ["Overflow"] = "Überlauf",
        ["Scroll timing"] = "Scroll-Timing",
        ["Step (ms)"] = "Schritt (ms)",
        ["Edge pause (ms)"] = "Randpause (ms)",
        ["Enabled"] = "Aktiviert",
        ["Insert template variable"] = "Vorlagenvariable einfügen",
        ["Live VFD preview"] = "Live-VFD-Vorschau",
        ["Connected media players"] = "Verbundene Medienplayer",
        ["Windows Media Player"] = "Windows Media Player",
        ["Nothing playing"] = "Keine Wiedergabe",
        ["Playback"] = "Wiedergabe",
        ["Playlist"] = "Wiedergabeliste",
        ["Audio"] = "Audio",
        ["Bitrate / sample rate"] = "Bitrate / Abtastrate",
        ["Current position / total tracks"] = "Position / Titelanzahl",
        ["VU / Spectrum"] = "Pegel / Spektrum",
        ["Connection"] = "Verbindung",
        ["Token"] = "Token",
        ["Home Assistant entities"] = "Home-Assistant-Entitäten",
        ["Entity"] = "Entität",
        ["Friendly name"] = "Anzeigename",
        ["State"] = "Zustand",
        ["Selected entity"] = "Ausgewählte Entität",
        ["Alias"] = "Alias",
        ["Attributes"] = "Attribute",
        ["Fan outputs"] = "Lüfterausgänge",
        ["Four channels / hardware-safe"] = "Vier Kanäle / hardwaregeschützt",
        ["Channel"] = "Kanal",
        ["Mode"] = "Modus",
        ["Sensor key"] = "Sensorschlüssel",
        ["Fixed percent"] = "Fester Prozentwert",
        ["Minimum / maximum"] = "Minimum / Maximum",
        ["Fail-safe percent"] = "Sicherheitswert (%)",
        ["Hysteresis (C)"] = "Hysterese (°C)",
        ["Allow stop"] = "Stillstand erlauben",
        ["Simulated sensor temperature"] = "Simulierte Sensortemperatur",
        ["Hardware monitoring"] = "Hardwareüberwachung",
        ["Read-only sensors from the current operating system. Refresh to capture a new snapshot; no LIS2 hardware commands are sent."] = "Schreibgeschützte Systemsensoren. Aktualisieren liest neue Werte ein; es werden keine LIS2-Hardwarebefehle gesendet.",
        ["Select a sensor above."] = "Bitte oben einen Sensor auswählen.",
        ["Read-only sensors from the current operating system. Refresh to capture a new snapshot; no LIS2 hardware commands are sent."] = "Schreibgeschützte Sensorwerte des Betriebssystems. Aktualisieren liest neue Messwerte ein; es werden keine LIS2-Hardwarebefehle gesendet.",
        ["Select a sensor above."] = "Oben einen Sensor auswählen.",
        ["Sensor"] = "Sensor",
        ["Type"] = "Typ",
        ["Value"] = "Wert",
        ["Selected sensor / fan assignment"] = "Sensordetails / Lüfterzuordnung",
        ["Display presets"] = "Displayvorlagen",
        ["Diagnostics Snapshot"] = "Diagnoseübersicht",
        ["Virtual LIS2 state"] = "Virtueller LIS2-Status",
        ["Decoded command history"] = "Dekodierter Befehlsverlauf",
        ["Protocol Log"] = "Protokoll",
        ["Priority"] = "Priorität",
        ["Active / pending events"] = "Aktive / wartende Ereignisse",
        ["Active / queued events"] = "Aktive / wartende Ereignisse",
        ["Device / Transport"] = "Gerät / Transport",
        ["Appearance"] = "Darstellung",
        ["Language"] = "Sprache",
        ["Theme"] = "Design",
        ["Manual fan outputs require an authorized LIS2 connection. Use Fan Control to configure and authorize hardware operation."] =
            "Manuelle Lüfterausgänge erfordern eine autorisierte LIS2-Verbindung. Konfiguration und Freigabe erfolgen unter Lüftersteuerung.",
        ["💡  Tip: Use the navigation on the left to access display settings, page editor, Winamp integration, hardware and fan control."] =
            "💡  Tipp: Über die Navigation links erreichst du Anzeigeeinstellungen, Seiteneditor, Winamp, Hardware und Lüftersteuerung.",
        ["Fan 1"] = "Lüfter 1",
        ["Fan 2"] = "Lüfter 2",
        ["Fan 3"] = "Lüfter 3",
        ["Fan 4"] = "Lüfter 4",
        ["COM port"] = "COM-Port"
    };

    private readonly Dictionary<Button, string> _originalPageButtons = new();

    private static readonly Dictionary<string, string> GermanButtonLabels = new(StringComparer.Ordinal)
    {
        ["Change..."] = "Ändern...",
        ["Create from preset"] = "Aus Vorlage erstellen",
        ["Send demo display"] = "Demoanzeige senden",
        ["Open Display Controls"] = "Displaysteuerung öffnen",
        ["Open Fan Control"] = "Lüftersteuerung öffnen",
        ["Apply"] = "Anwenden",
        ["Send"] = "Senden",
        ["Render both lines"] = "Beide Zeilen anzeigen",
        ["Clear"] = "Löschen",
        ["Send special-character test"] = "Sonderzeichentest senden",
        ["Previous"] = "Zurück",
        ["Next"] = "Weiter",
        ["Send 40 bytes"] = "40 Bytes senden",
        ["Save glyph"] = "Zeichen speichern",
        ["Send slot to LIS2"] = "Speicherplatz an LIS2 senden",
        ["Send all 8 slots"] = "Alle 8 Speicherplätze senden",
        ["Load set..."] = "Zeichensatz laden...",
        ["Save set..."] = "Zeichensatz speichern...",
        ["Load examples"] = "Beispiele laden",
        ["Add"] = "Hinzufügen",
        ["Duplicate"] = "Duplizieren",
        ["Delete"] = "Löschen",
        ["Load pages..."] = "Seiten laden...",
        ["Save pages..."] = "Seiten speichern...",
        ["↑  Move up"] = "↑  Nach oben",
        ["↓  Move down"] = "↓  Nach unten",
        ["Apply condition"] = "Bedingung anwenden",
        ["Insert into line 1"] = "In Zeile 1 einfügen",
        ["Insert into line 2"] = "In Zeile 2 einfügen",
        ["Refresh variables"] = "Variablen aktualisieren",
        ["Save page"] = "Seite speichern",
        ["Test event (5 s)"] = "Ereignis testen (5 s)",
        ["Start sequence"] = "Sequenz starten",
        ["Stop sequence"] = "Sequenz stoppen",
        ["Create playback page"] = "Wiedergabeseite erstellen",
        ["Test connection"] = "Verbindung testen",
        ["Save / reconnect"] = "Speichern / neu verbinden",
        ["Save entity"] = "Entität speichern",
        ["Copy key"] = "Schlüssel kopieren",
        ["Create display page"] = "Displayseite erstellen",
        ["Create attribute page"] = "Attributseite erstellen",
        ["Save fan channel"] = "Lüfterkanal speichern",
        ["Simulate all four channels"] = "Alle vier Kanäle simulieren",
        ["List detected sensors"] = "Erkannte Sensoren anzeigen",
        ["Preview with real sensor values (dry run)"] = "Mit echten Sensorwerten simulieren",
        ["Start continuous sensor preview (dry run)"] = "Laufende Sensorsimulation starten",
        ["Stop preview"] = "Vorschau stoppen",
        ["Start virtual four-fan controller"] = "Virtuelle Vier-Lüfter-Steuerung starten",
        ["Stop and restore safety frame"] = "Stoppen und Sicherheitswerte wiederherstellen",
        ["Enable physical four-fan control"] = "Physische Vier-Lüfter-Steuerung aktivieren",
        ["Stop and restore 100% safety frame"] = "Stoppen und 100-%-Sicherheitswerte setzen",
        ["Refresh sensors"] = "Sensoren aktualisieren",
        ["Start live updates"] = "Live-Aktualisierung starten",
        ["Save sensor preference"] = "Sensoreinstellungen speichern",
        ["Use for Fan 1"] = "Für Lüfter 1 verwenden",
        ["Fan 2"] = "Lüfter 2",
        ["Fan 3"] = "Lüfter 3",
        ["Fan 4"] = "Lüfter 4",
        ["Memory"] = "Arbeitsspeicher",
        ["Temperatures"] = "Temperaturen",
        ["Fans"] = "Lüfter",
        ["Export report..."] = "Bericht exportieren...",
        ["Refresh"] = "Aktualisieren",
        ["Show event"] = "Ereignis anzeigen",
        ["Test warning"] = "Warnung testen",
        ["Clear all"] = "Alle löschen",
        ["Cancel selected"] = "Ausgewähltes abbrechen",
        ["Apply / reconnect"] = "Anwenden / neu verbinden",
        ["Import Windows settings.json..."] = "Windows-settings.json importieren...",
        ["Refresh serial ports"] = "Serielle Ports aktualisieren",
        ["Connect serial display"] = "Serielles Display verbinden",
        ["Disconnect serial"] = "Serielle Verbindung trennen",
        ["Send display lines"] = "Displayzeilen senden",
        ["Start serial page sequence"] = "Serielle Seitensequenz starten",
        ["Stop serial page sequence"] = "Serielle Seitensequenz stoppen",
        ["Send selected page"] = "Ausgewählte Seite senden",
        ["Program 8 glyphs"] = "8 Zeichen programmieren",
        ["Clear serial display"] = "Serielles Display löschen",
        ["Connect virtual LIS2"] = "Virtuelles LIS2 verbinden",
        ["Disconnect"] = "Trennen"
    };

    private readonly Dictionary<CheckBox, string> _originalPageCheckBoxes = new();

    private static readonly Dictionary<string, string> GermanCheckBoxLabels = new(StringComparer.Ordinal)
    {
        ["Automatic control"] = "Automatische Steuerung",
        ["Favorites only"] = "Nur Favoriten",
        ["Favorite"] = "Favorit",
        ["Favorites only"] = "Nur Favoriten",
        ["Favorite"] = "Favorit",
        ["Enable or disable this page"] = "Seite aktivieren oder deaktivieren",
        ["I explicitly authorize physical fan output for this connection"] =
            "Ich autorisiere ausdrücklich die physischen Lüfterausgänge für diese Verbindung"
    };

    private readonly Dictionary<TextBox, string> _originalWatermarks = new();
    private readonly Dictionary<ComboBoxItem, string> _originalComboLabels = new();

    private IEnumerable<T> LocalizableControls<T>() where T : global::Avalonia.Visual
    {
        // Some Avalonia controls exist only in the logical tree, others are
        // instantiated by templates in the visual tree. WPF walks both.
        return this.GetLogicalDescendants().OfType<T>()
            .Concat(this.GetVisualDescendants().OfType<T>())
            .Distinct();
    }

    private void ApplyPageLocalization()
    {
        // Logical descendants include controls in inactive/hidden pages and do not
        // depend on the visual template being attached at the moment of translation.
        // Restrict translations to a known set of static labels. Never overwrite live
        // sensor values, media metadata, Home Assistant states or error messages.
        foreach (var input in LocalizableControls<TextBox>())
        {
            if (!_originalWatermarks.TryGetValue(input, out var original))
            {
                original = input.Watermark ?? string.Empty;
                _originalWatermarks[input] = original;
            }
            input.Watermark = WpfTranslationCatalog.Translate(original, CurrentUiLanguage());
        }
        foreach (var item in LocalizableControls<ComboBoxItem>())
        {
            if (item.Content is not string current) continue;
            if (!_originalComboLabels.TryGetValue(item, out var original))
            {
                original = current;
                _originalComboLabels[item] = original;
            }
            item.Content = WpfTranslationCatalog.Translate(original, CurrentUiLanguage());
        }
        foreach (var label in LocalizableControls<TextBlock>())
        {
            if (!_originalPageLabels.TryGetValue(label, out var original))
            {
                original = label.Text ?? string.Empty;
                if (!GermanPageLabels.ContainsKey(original) && WpfTranslationCatalog.Translate(original, CurrentUiLanguage()) == original) continue;
                _originalPageLabels.Add(label, original);
            }
            label.Text = CurrentUiLanguage() == "de" && GermanPageLabels.TryGetValue(original, out var translated)
                ? translated : WpfTranslationCatalog.Translate(original, CurrentUiLanguage());
        }
        foreach (var checkBox in LocalizableControls<CheckBox>())
        {
            if (!_originalPageCheckBoxes.TryGetValue(checkBox, out var original))
            {
                if (checkBox.Content is not string content || (!GermanCheckBoxLabels.ContainsKey(content) && WpfTranslationCatalog.Translate(content, CurrentUiLanguage()) == content))
                    continue;
                original = content;
                _originalPageCheckBoxes.Add(checkBox, original);
            }
            checkBox.Content = CurrentUiLanguage() == "de" && GermanCheckBoxLabels.TryGetValue(original, out var translated)
                ? translated : WpfTranslationCatalog.Translate(original, CurrentUiLanguage());
        }
        foreach (var button in LocalizableControls<Button>())
        {
            if (!_originalPageButtons.TryGetValue(button, out var original))
            {
                if (button.Content is not string text || (!GermanButtonLabels.ContainsKey(text) && WpfTranslationCatalog.Translate(text, CurrentUiLanguage()) == text))
                    continue;
                original = text;
                _originalPageButtons.Add(button, original);
            }
            button.Content = CurrentUiLanguage() == "de" && GermanButtonLabels.TryGetValue(original, out var translated)
                ? translated : WpfTranslationCatalog.Translate(original, CurrentUiLanguage());
        }
    }
}
