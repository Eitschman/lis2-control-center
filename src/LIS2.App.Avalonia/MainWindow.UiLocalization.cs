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
        ["Device / Transport"] = "Gerät / Transport",
        ["Appearance"] = "Darstellung",
        ["Language"] = "Sprache",
        ["Theme"] = "Design",
        ["COM port"] = "COM-Port"
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
    }
}
