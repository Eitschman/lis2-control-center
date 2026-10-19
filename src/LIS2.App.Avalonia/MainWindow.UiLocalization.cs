using Avalonia.Controls;
using Avalonia.VisualTree;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    // Keep the original UI strings, so language changes are reversible without reloading a window.
    private readonly Dictionary<TextBlock, string> _originalPageLabels = new();

    private static readonly Dictionary<string, string> GermanPageLabels = new(StringComparer.Ordinal)
    {
        ["VFD Preview"] = "VFD-Vorschau",
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
        ["COM port"] = "COM-Port"
    };

    private readonly Dictionary<Button, string> _originalPageButtons = new();

    private static readonly Dictionary<string, string> GermanButtonLabels = new(StringComparer.Ordinal)
    {
        ["Change..."] = "Ändern...",
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

    private void ApplyPageLocalization()
    {
        // Restrict translations to a known set of static labels. Never overwrite live
        // sensor values, media metadata, Home Assistant states or error messages.
        foreach (var label in this.GetVisualDescendants().OfType<TextBlock>())
        {
            if (!_originalPageLabels.TryGetValue(label, out var original))
            {
                original = label.Text ?? string.Empty;
                if (!GermanPageLabels.ContainsKey(original)) continue;
                _originalPageLabels.Add(label, original);
            }
            label.Text = CurrentUiLanguage() == "de" &&
                GermanPageLabels.TryGetValue(original, out var translated) ? translated : original;
        }
        foreach (var button in this.GetVisualDescendants().OfType<Button>())
        {
            if (!_originalPageButtons.TryGetValue(button, out var original))
            {
                if (button.Content is not string text || !GermanButtonLabels.ContainsKey(text))
                    continue;
                original = text;
                _originalPageButtons.Add(button, original);
            }
            button.Content = CurrentUiLanguage() == "de" &&
                GermanButtonLabels.TryGetValue(original, out var translated) ? translated : original;
        }
    }
}
