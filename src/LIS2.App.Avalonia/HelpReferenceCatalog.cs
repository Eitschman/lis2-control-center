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
        int index, IEnumerable<string> variableKeys, IEnumerable<string> glyphNames, string language = "en")
    {
        var content = new List<(string, string)>();
        void Add(string heading, string body) => content.Add((
            HelpReferenceLocalization.Translate(heading, language), HelpReferenceLocalization.Translate(body, language)));
        // Beispiele und dynamische Schlüssel sind ausführbare Vorlagensyntax, keine übersetzbaren Texte.
        void AddExample(string heading, string body) => content.Add((HelpReferenceLocalization.Translate(heading, language), body));
        switch (ClampSection(index))
        {
            case 0:
                Add("Dashboard", "The Dashboard shows the current 20x2 VFD preview, connection status, recent commands and direct fan outputs.");
                Add("VFD preview", "The preview displays the normalized 20-character lines. Long text may scroll during page playback.");
                Add("Fan outputs", "Manual output respects the minimum, maximum and AllowStop of all four channels. Each serial action needs fresh authorization. Authorized serial fan output sends 100% first; stop or orderly disconnect sends 100%. Manual serial output is blocked during automatic serial control.");
                Add("Quick links", "Open Display for manual output or Fan Control for regulation and sensor assignments.");
                break;
            case 1:
                Add("Display", "Use the Display section for direct physical or virtual LIS2 output. Manual commands interrupt page scheduling.");
                Add("Lines and brightness", "Lines, characters and brightness use the applied transport. Brightness supports 100%, 75%, 50% and 25%, is saved and reapplied on connection. The serial preview shows sent content, without feedback from the physical display.");
                Add("Custom glyphs", "Eight programmable 5x8 slots are available. Use {Glyph.1} through {Glyph.8} in templates.");
                foreach (var glyph in glyphNames.Take(8).Select((name, index) => (name, slot: index + 1))
                             .Where(glyph => !string.IsNullOrWhiteSpace(glyph.name)))
                {
                    AddExample("Named glyph", glyph.name + ": {Glyph." + glyph.slot + "}");
                    var key = PageSourceValues.NormalizeGlyphName(glyph.name);
                    if (key.Length > 0 && !int.TryParse(key, out _))
                        AddExample("Named glyph", "{Glyph." + key + "}");
                }
                Add("Character ROM", "The raw ROM and special-character tests are intended for protocol validation on real hardware.");
                Add("Custom glyphs", "Save glyphs in the workspace and send them to the connected device. Programming on connect restores them after reconnecting. Templates accept numeric slots and saved names, for example {Glyph.1} and {Glyph.Play}. Spaces and punctuation become underscores; duplicate names use the last slot. Numeric names do not replace numeric slots.");
                break;
            case 2:
                Add("Pages", "Each enabled page contains a name, two templates, duration, refresh interval, overflow modes and optional visibility condition.");
                Add("Timing", "Duration is in seconds; refresh and scrolling use milliseconds. Overflow can truncate, marquee or ping-pong.");
                AddExample("Template syntax", "{Variable}  {Variable|fallback:--}  {Variable|number:1}  {Variable|percent:0}  {Variable|bytes:1}");
                Add("Formatting", "Operations may be chained: prefix, suffix, upper, lower, fallback, number, percent and bytes.");
                Add("Visibility", "Use =, !=, >, >=, < or <=. Example: HA.sensor.temperature>=25");
                Add("Formatting details", "number and percent accept 0 to 6 decimal places. percent appends % without multiplying by 100; bytes uses powers of 1024. Numbers follow the operating system culture. Missing values return fallback directly, without further formatting. Visibility uses one condition without braces; numeric comparisons use a decimal point. Missing keys hide the page.");
                AddExample("Clock", "{Clock.Time}  {Clock.Date}  {Clock.Day}");
                content.AddRange(HelpVariableCatalog.Get("Clock.", language));
                var keys = variableKeys.Where(k => !k.StartsWith("HA.", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k => k, StringComparer.OrdinalIgnoreCase).Take(200).ToArray();
                if (keys.Length == 0) Add("Available runtime variables", "No runtime variables currently available.");
                else AddExample("Available runtime variables", string.Join(Environment.NewLine, keys.Select(k => "{" + k + "}")));
                Add("Finding keys", "This list shows up to 200 current non-HA keys. Browse variables searches the current snapshot and shows at most 50 matches; narrow the search or refresh it after connecting a source. Copy a key or insert it into a line. Save page changes before starting playback. Only enabled pages with a matching visibility condition participate in rotation.");
                Add("Home Assistant variables", "Use the Home Assistant browser for the full live entity and attribute catalogue rather than listing thousands of entries here.");
                break;
            case 3:
                Add("Media", "Connected players supply read-only playback status, track metadata, elapsed time, duration and audio telemetry where supported.");
                AddExample("Winamp", "{Winamp.State}  {Winamp.Artist}  {Winamp.Title}  {Winamp.Album}  {Winamp.Elapsed}  {Winamp.Duration}");
                Add("Spectrum", "{Winamp.Vu}  {Winamp.Spectrum}  {Winamp.SpectrumGlyphs} require a supported source of live audio telemetry.");
                AddExample("Windows media", "{WindowsMedia.State}  {WindowsMedia.Artist}  {WindowsMedia.Title}  {WindowsMedia.Connected}");
                Add("Linux and Windows", "Linux uses MPRIS through playerctl, which must be installed. Select an automatic or specific player; the selection is saved. An unavailable selected player stays disconnected. Media.* provides Player, State, Title, Artist, Album, Elapsed, Duration and Connected, but no VU or spectrum. Values are refreshed every second. Windows uses WindowsMedia.* for GSMTC and Winamp.* for the Winamp plug-in. WMP Legacy is deferred and is not started. Create a page from the relevant media card. No plug-in is needed for GSMTC.");
                AddExample("Media", "{Media.Player}  {Media.State}  {Media.Title}  {Media.Artist}  {Media.Album}  {Media.Elapsed}  {Media.Duration}  {Media.Connected}");
                AddExample("Example", "{Winamp.Artist|fallback:Nothing playing} / {Winamp.Title|fallback:--}");
                content.AddRange(HelpVariableCatalog.Get("Media.", language));
                content.AddRange(HelpVariableCatalog.Get("Winamp.", language));
                content.AddRange(HelpVariableCatalog.Get("WindowsMedia.", language));
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
                Add("Regulation", "Fixed uses the configured percentage. Curve interpolates temperature=percent points and applies hysteresis. Follow rounds the sensor value to a percentage. External uses the fail-safe value because this application supplies no external control percentage. Off outputs zero only with AllowStop; otherwise it uses the minimum. Minimum and maximum limit normal outputs.");
                Add("Fail-safe", "Missing or invalid sensors in Curve and Follow use the configured fail-safe value within the channel limits. Preview and simulation do not control physical fans. Physical output requires explicit authorization for the current serial connection. Start sends 100% first; stop and orderly disconnect send 100%. A broken physical connection cannot guarantee delivery of a safety frame.");
                break;
            case 7:
                Add("Settings", "Select Virtual LIS2 or a serial port. Configure language, appearance and device startup preferences.");
                Add("Transport", "Applying saves the transport and port, disconnects the current device and connects the selected transport. A missing saved port remains selected and is never replaced automatically. Serial display control does not enable fans.");
                Add("Windows import", "Import in Settings reads settings.json without changing the source file. It imports transport, port, brightness, glyphs, pages, fans and sensor preferences, including Home Assistant activation and URL. The token stays separate. Reconnect Home Assistant afterward if required.");
                Add("Appearance", "The system, light and dark themes are supported. Language changes apply to the application interface.");
                Add("Settings", "Autostart uses the Windows user registry or a Linux XDG autostart entry. Windows closes to tray; Linux closes the application and keeps minimized windows in the taskbar. Tray menus offer Open and Exit. A second launch activates the existing instance. F1 opens help for the current section.");
                break;
            case 8:
                Add("Diagnostics", "Inspect the current device snapshot, decoded command history and protocol log.");
                Add("Export", "Export diagnostic information for troubleshooting or hardware-validation sessions.");
                Add("Virtual LIS2", "The virtual device permits protocol tests without physical hardware.");
                break;
            case 9:
                Add("Home Assistant", "The read-only WebSocket integration loads states and receives live state_changed updates.");
                Add("Connection", "Enter the Home Assistant URL and a long-lived access token, then test and save the connection.");
                Add("Token storage", "Windows uses DPAPI. Linux uses the desktop keyring through secret-tool. Local AES storage requires explicit selection; the key is stored beside the encrypted token. Keyring errors are displayed without switching storage automatically. Disabling Home Assistant stops the source and removes the token from the selected store. Switching back to the keyring migrates local files only after verifying the saved token.");
                AddExample("Entity state", "{HA.sensor.living_room_temperature}");
                AddExample("Unit and friendly name", "{HA.sensor.living_room_temperature.Unit}  {HA.sensor.living_room_temperature.FriendlyName}");
                AddExample("Attributes", "{HA.media_player.living_room.attribute.media_title}");
                AddExample("Aliases", "{HA.LivingRoomTemp}  {HA.LivingRoomTemp.Unit}  {HA.LivingRoomTemp.attribute.device_class}");
                AddExample("Formatting", "{HA.sensor.temperature|fallback:--|number:1|suffix:°C}");
                AddExample("Visibility", "HA.binary_sensor.window=on  /  HA.sensor.temperature>=25");
                Add("Unavailable values", "Unknown and unavailable states can be handled with fallback. Complex attributes are represented as compact JSON.");
                Add("Finding keys", "Search the Home Assistant browser by entity ID, friendly name or alias, then inspect attributes and copy the exact template key.");
                break;
        }
        return content;
    }

}
