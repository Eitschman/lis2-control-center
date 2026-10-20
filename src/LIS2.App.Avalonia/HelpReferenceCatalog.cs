using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS2.App.Avalonia;

/// <summary>Cross-platform help content, independent of Avalonia controls and live device access.</summary>
internal static class HelpReferenceCatalog
{
    internal static readonly string[] Sections =
    [
        "Dashboard", "Display", "Pages", "Media", "Events", "Hardware",
        "Fan Control", "Settings", "Diagnostics", "Home Assistant"
    ];

    internal static int ClampSection(int index) => Math.Clamp(index, 0, Sections.Length - 1);

    internal static IReadOnlyList<(string Heading, string Body)> GetSection(
        int index, IEnumerable<string> variableKeys, IEnumerable<string> glyphNames)
    {
        var content = new List<(string, string)>();
        void Add(string heading, string body) => content.Add((heading, body));
        switch (ClampSection(index))
        {
            case 0:
                Add("Dashboard", "The Dashboard shows the current 20x2 VFD preview, connection status, recent commands and direct fan outputs.");
                Add("VFD preview", "The preview displays the normalized 20-character lines. Long text may scroll during page playback.");
                Add("Fan outputs", "Manuelle Ausgaben beachten Minimum, Maximum und AllowStop der vier Kanäle. Bei serieller Ausgabe ist jede Aktion erneut freizugeben. Die erste Ausgabe setzt 100 Prozent; Trennen oder Stoppen stellt 100 Prozent wieder her. Während automatischer Regelung bleibt die Handausgabe gesperrt.");
                Add("Quick links", "Open Display for manual output or Fan Control for regulation and sensor assignments.");
                break;
            case 1:
                Add("Display", "Use the Display section for direct physical or virtual LIS2 output. Manual commands interrupt page scheduling.");
                Add("Lines and brightness", "Zeilen, Zeichen und Helligkeit werden an den angewendeten Transport gesendet. Helligkeit unterstützt 100%, 75%, 50% und 25%, wird gespeichert und beim Verbinden erneut angewendet. Die serielle Vorschau zeigt gesendete Inhalte, keine Display-Rückmeldung.");
                Add("Custom glyphs", "Eight programmable 5x8 slots are available. Use {Glyph.1} through {Glyph.8} in templates.");
                foreach (var name in glyphNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                    Add("Named glyph", "{Glyph." + NormalizeGlyphName(name) + "}");
                Add("Character ROM", "The raw ROM and special-character tests are intended for protocol validation on real hardware.");
                break;
            case 2:
                Add("Pages", "Each enabled page contains a name, two templates, duration, refresh interval, overflow modes and optional visibility condition.");
                Add("Timing", "Duration is in seconds; refresh and scrolling use milliseconds. Overflow can truncate, marquee or ping-pong.");
                Add("Template syntax", "{Variable}  {Variable|fallback:--}  {Variable|number:1}  {Variable|percent:0}  {Variable|bytes:1}");
                Add("Formatting", "Operations may be chained: prefix, suffix, upper, lower, fallback, number, percent and bytes.");
                Add("Visibility", "Use =, !=, >, >=, < or <=. Example: HA.sensor.temperature>=25");
                Add("Clock", "{Clock.Time}  {Clock.Date}  {Clock.Day}");
                var keys = variableKeys.Where(k => !k.StartsWith("HA.", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k => k, StringComparer.OrdinalIgnoreCase).Take(200).ToArray();
                Add("Available runtime variables", keys.Length == 0
                    ? "No runtime variables currently available."
                    : string.Join(Environment.NewLine, keys.Select(k => "{" + k + "}")));
                Add("Home Assistant variables", "Use the Home Assistant browser for the full live entity and attribute catalogue rather than listing thousands of entries here.");
                break;
            case 3:
                Add("Media", "Connected players supply read-only playback status, track metadata, elapsed time, duration and audio telemetry where supported.");
                Add("Winamp", "{Winamp.State}  {Winamp.Artist}  {Winamp.Title}  {Winamp.Album}  {Winamp.Elapsed}  {Winamp.Duration}");
                Add("Spectrum", "{Winamp.Vu}  {Winamp.Spectrum}  {Winamp.SpectrumGlyphs} require a supported source of live audio telemetry.");
                Add("Windows media", "{WindowsMedia.State}  {WindowsMedia.Artist}  {WindowsMedia.Title}  {WindowsMedia.Connected}");
                Add("Example", "{Winamp.Artist|fallback:Nothing playing} / {Winamp.Title|fallback:--}");
                break;
            case 4:
                Add("Events", "Timed events temporarily override ordinary page output on connected displays.");
                Add("Priorities", "Critical events preempt warnings, notices and information messages. The highest priority active event is displayed.");
                Add("Lifetime", "An event expires automatically. Cancel or clear it in the Events view; ordinary page playback resumes afterward.");
                break;
            case 5:
                Add("Hardware", "The hardware browser exposes read-only sensors from the current operating system.");
                Add("Sensor variables", "Use the exact Hardware.* key from the sensor browser in a page template.");
                Add("Linux and Windows", "Available temperature, utilization, fan and storage sensors depend on the host and installed drivers.");
                break;
            case 6:
                Add("Fan Control", "Configure four channels, each with sensor assignment, mode and safe minimum/maximum output.");
                Add("Regulation", "Choose a fixed percentage or temperature-based control with hysteresis.");
                Add("Fail-safe", "Loss of a sensor or connection invokes the configured fail-safe output. Physical fan control requires explicit authorization.");
                break;
            case 7:
                Add("Settings", "Select Virtual LIS2 or a serial port. Configure language, appearance and device startup preferences.");
                Add("Transport", "Transport und Port werden beim Anwenden gespeichert. Die Anwendung trennt die bisherige Verbindung und verbindet den gewählten Transport neu. Ein fehlender gespeicherter Port bleibt ausgewählt und wird nicht automatisch ersetzt. Serielle Displaybedienung aktiviert keine Lüfter.");
                Add("Windows-Import", "Der sichtbare Import in den Einstellungen liest settings.json, ohne die Quelldatei zu ändern. Transport, Port, Helligkeit, Glyphen, Seiten, Lüfter und Sensorpräferenzen werden übernommen. Home-Assistant-Aktivierung und URL werden übernommen, der Token bleibt getrennt. Home Assistant danach bei Bedarf neu verbinden.");
                Add("Appearance", "The system, light and dark themes are supported. Language changes apply to the application interface.");
                Add("Windows startup", "Automatic startup is only available on Windows.");
                break;
            case 8:
                Add("Diagnostics", "Inspect the current device snapshot, decoded command history and protocol log.");
                Add("Export", "Export diagnostic information for troubleshooting or hardware-validation sessions.");
                Add("Virtual LIS2", "The virtual device permits protocol tests without physical hardware.");
                break;
            case 9:
                Add("Home Assistant", "The read-only WebSocket integration loads states and receives live state_changed updates.");
                Add("Connection", "Enter the Home Assistant URL and a long-lived access token, then test and save the connection.");
                Add("Entity state", "{HA.sensor.living_room_temperature}");
                Add("Unit and friendly name", "{HA.sensor.living_room_temperature.Unit}  {HA.sensor.living_room_temperature.FriendlyName}");
                Add("Attributes", "{HA.media_player.living_room.attribute.media_title}");
                Add("Aliases", "{HA.LivingRoomTemp}  {HA.LivingRoomTemp.Unit}  {HA.LivingRoomTemp.attribute.device_class}");
                Add("Formatting", "{HA.sensor.temperature|fallback:--|number:1|suffix:°C}");
                Add("Visibility", "HA.binary_sensor.window=on  /  HA.sensor.temperature>=25");
                Add("Unavailable values", "Unknown and unavailable states can be handled with fallback. Complex attributes are represented as compact JSON.");
                Add("Finding keys", "Search the Home Assistant browser by entity ID, friendly name or alias, then inspect attributes and copy the exact template key.");
                break;
        }
        return content;
    }

    internal static string NormalizeGlyphName(string value) =>
        new string(value.Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_').ToArray()).Trim('_');
}
