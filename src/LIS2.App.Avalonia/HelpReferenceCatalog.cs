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

    internal static IReadOnlyList<(string Heading, string Body, bool IsExample)> GetSection(
        int index, IEnumerable<string> variableKeys, IEnumerable<string> glyphNames, string language = "en")
    {
        var content = new List<(string, string, bool)>();
        void Add(string heading, string body) => content.Add((
            HelpReferenceLocalization.Translate(heading, language), HelpReferenceLocalization.Translate(body, language), false));
        // Beispiele und dynamische Schlüssel sind ausführbare Vorlagensyntax, keine übersetzbaren Texte.
        void AddExample(string heading, string body) => content.Add((HelpReferenceLocalization.Translate(heading, language), body, true));
        switch (ClampSection(index))
        {
            case 0:
                Add("Dashboard", "The Dashboard combines the compact 20x2 VFD preview, page controls, four fan channels and eight configurable live values. Connection status remains in the header.");
                Add("Regional numbers", "All source numbers use the operating system regional settings, independently of the interface language. Hardware, Home Assistant and variable browsers use the same presentation. Numeric Home Assistant states retain their decimal places without an explicit format. Titles, text attributes, dates, timelines, source keys and technical input syntax remain unchanged. Use number, percent or bytes in a template to choose precision.");
                Add("VFD preview", "The preview displays the normalized 20-character lines. Long text may scroll during page playback.");
                Add("Fan outputs", "Manual output respects the minimum, maximum and AllowStop of all four channels. Each serial action needs fresh authorization. Authorized serial fan output sends 100% first; stop or orderly disconnect sends 100%. Manual serial output is blocked during automatic serial control.");
                Add("Current page", "Choose an enabled, currently visible page or use Previous and Next while the sequence is running. Pause stops only automatic page rotation; live values, scrolling and event overlays continue. Resume continues rotation. These controls use the applied virtual or serial transport; they are disabled when playback is stopped.");
                Add("Fan outputs", "Each channel shows its configured mode and control sensor. Use the gear beside Fan Outputs to assign a real RPM sensor to each channel. The LIS2 does not supply confirmed RPM feedback. Unassigned speed sensors show —; configured sensors without data show Unavailable. Assigning a sensor never authorizes hardware output.");
                Add("Live values", "Use the gear beside Live values to configure eight labels and templates. Browse variables inserts a current source key; existing template formatting is supported, for example {Hardware.Cpu.Temperature|number:1|suffix: °C}. Choose actual keys from your system. Unconfigured slots show —; configured templates without results show Unavailable. Compact numbers changes explicit numeric formats to at most one decimal place and adds spaces before units in the draft; Save applies the changes. Existing templates remain unchanged unless you choose this action. Save persists the selection; Cancel discards edits. These values are independent of the active display page.");
                break;
            case 1:
                Add("Display", "Use the Display section for direct physical or virtual LIS2 output. Manual commands interrupt page scheduling.");
                Add("Lines and brightness", "Lines, characters and brightness use the applied transport. Brightness supports 100%, 75%, 50% and 25%, is saved and reapplied on connection. The serial preview shows sent content, without feedback from the physical display.");
                Add("Custom glyphs", "Eight programmable 5x8 slots are available. Use {Glyph.1} through {Glyph.8} in templates.");
                Add("Custom glyphs", "Click pixels or edit the eight binary rows. With programming on connect enabled, loading a glyph set also programs the connected display. Saved and sent states are separate. Previews show slot markers or spectrum bars.");
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
                Add("Template validation", SourceQualityText.ValidationHelp);
                Add("Pages", "Each enabled page contains a name, two templates, duration, refresh interval, overflow modes and optional visibility condition.");
                Add("Timing", "Duration is in seconds; refresh and scrolling use milliseconds. Overflow can truncate, marquee or ping-pong.");
                AddExample("Template syntax", "{Variable}  {Variable|fallback:--}  {Variable|number:1}  {Variable|percent:0}  {Variable|bytes:1}");
                Add("Formatting", "Operations may be chained: prefix, suffix, upper, lower, fallback, fallbackvar, number, percent and bytes.");
                Add("Fallback variable", "Use fallbackvar with a variable key without braces. The first available value is used: the main variable, then fallback variables from left to right, then the literal fallback text. Missing, empty, unknown and unavailable values are skipped; zero, false and spaces remain valid. Variable values receive the formatting pipeline; literal fallback text is returned unchanged. Variable values are not evaluated as templates. In the formatting editor, choose Fallback variable with its browser and enter Missing value as the final text.");
                AddExample("Example", "{Winamp.Artist|fallbackvar:Winamp.StreamName|fallback:--}\n{Value|fallbackvar:Other.Value|number:2|suffix:°C|fallback:--}");
                Add("Formatting editor", "Expand Formatting editor on Pages. Enter a variable key without braces or select it with Browse variables. Choose Unchanged, Number, Percent or Bytes, set 0 to 6 decimal places and optionally choose letter case, prefix, suffix and missing-value text. Template shows the generated expression; Current result uses live source values. Insert into line 1 or 2 inserts at that field's caret. Save page to persist the changed template; the helper settings themselves are not saved. Free editing remains available. F1 in either line field opens its formatting hint; Escape closes it.");
                Add("Formatting examples", "In these examples, replace Value with an actual variable key from Browse variables. The value before the arrow is source data, not text to enter in the line. Decimal commas follow German regional settings; English regional settings use a decimal point. Changing the app language does not change the operating system number format. Prefix and suffix are added after letter case. The editor rejects braces and | in its fields because they delimit template syntax; variable keys cannot contain spaces.");
                AddExample("Numbers", "{Value|number:2}: 50.1234567 → 50,12 (de-DE) / 50.12 (en-US)\n{Value|number:0}: 50.1234567 → 50\n{Value|number:2|suffix:°C}: 50.1234567 → 50,12°C (de-DE)");
                AddExample("Percent", "{Value|percent:1}: 73.24 → 73,2% (de-DE)\n{Value|percent:0}: 0.75 → 1% (de-DE)");
                AddExample("Bytes", "{Value|bytes:2}: 1073741824 → 1,00 GB (de-DE) / 1.00 GB (en-US)");
                AddExample("Text", "{Value|upper|prefix:[|suffix:]}: lis2 → [LIS2]\n{Value|lower}: LIS2 → lis2\n{Value|percent:0|prefix:Core 10: }: 5 → Core 10: 5%\n{Value|suffix: %}: 5 → 5 %");
                Add("Missing values", "If Value is missing, unknown or unavailable, {Value|fallback:--|number:2|suffix:°C} displays --. The fallback is returned directly; it is not formatted and receives no suffix. A valid value of 50.1234567 displays 50,12°C with German regional settings. Percent only adds the % symbol; it does not multiply the source by 100. Bytes divides by powers of 1024.");
                Add("Visibility", "Use =, !=, >, >=, < or <=. Example: HA.sensor.temperature>=25");
                Add("Visibility", "The condition fields follow the raw expression and selected page. Browse variables selects a current key for the condition. Invalid raw expressions remain editable. Save feedback appears beside the editor; a failed save keeps the draft.");
                Add("Formatting details", "number and percent accept 0 to 6 decimal places. percent appends % without multiplying by 100; bytes uses powers of 1024. Numbers follow the operating system culture. Missing values return fallback directly, without further formatting. Visibility uses one condition without braces; numeric comparisons use a decimal point. Missing keys hide the page.");
                AddExample("Clock", "{Clock.Time}  {Clock.Date}  {Clock.Day}");
                content.AddRange(HelpVariableCatalog.Get("Clock.", language).Select(entry => (entry.Heading, entry.Body, false)));
                var keys = variableKeys.Where(k => !k.StartsWith("HA.", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k => k, StringComparer.OrdinalIgnoreCase).Take(200).ToArray();
                if (keys.Length == 0) Add("Available runtime variables", "No runtime variables currently available.");
                else AddExample("Available runtime variables", string.Join(Environment.NewLine, keys.Select(k => "{" + k + "}")));
                Add("Finding keys", "This list shows up to 200 current non-HA keys. Browse variables searches the current snapshot and shows at most 50 matches; narrow the search or refresh it after connecting a source. Copy a key or insert it into a line. Save page changes before starting playback. Only enabled pages with a matching visibility condition participate in rotation.");
                Add("Home Assistant variables", "Use the Home Assistant browser for the full live entity and attribute catalogue rather than listing thousands of entries here.");
                break;
            case 3:
                Add("Data freshness", SourceQualityText.FreshnessHelp);
                Add("Media", "Connected players supply read-only playback status, track metadata, elapsed time, duration and audio telemetry where supported.");
                AddExample("Winamp", "{Winamp.State}  {Winamp.Artist}  {Winamp.Title}  {Winamp.Album}  {Winamp.Elapsed}  {Winamp.Duration}");
                Add("Stream metadata", "Local files use their tags. Streams expose StreamName, StreamTitle, StreamGenre, StreamUrl and IsStream. Title prefers the current StreamTitle, then title metadata, then the playlist display text. Artist is never replaced automatically or split from StreamTitle. Missing decoder fields remain empty. Unicode is preserved. Diagnostics includes TitleSource. Install the matching new gen_lis2.dll; older plug-ins do not supply these fields.");
                AddExample("Example", "{Winamp.Artist|fallbackvar:Winamp.StreamName|fallback:--}\n{Winamp.Title|fallback:--}\nWinamp.IsStream = True");
                Add("Spectrum", "{Winamp.Vu}  {Winamp.Spectrum}  {Winamp.SpectrumGlyphs} require a supported source of live audio telemetry.");
                Add("Spectrum", "The Winamp card shows stereo levels, twenty spectrum bands, the pipe and last snapshot time. Disconnection clears metadata and levels. SpectrumGlyphs uses the active spectrum glyph set; other sets use the text spectrum.");
                AddExample("Windows media", "{WindowsMedia.State}  {WindowsMedia.Artist}  {WindowsMedia.Title}  {WindowsMedia.Connected}");
                Add("Modern player subtitles", "WindowsMedia.Subtitle exposes the subtitle supplied by GSMTC. It is not a guaranteed station name. Windows does not expose a separate station name or stream URL through this interface. Artist and Title remain unchanged. Select Subtitle explicitly as a fallback variable if useful; missing subtitles remain unavailable.");
                AddExample("Example", "{WindowsMedia.Artist|fallbackvar:WindowsMedia.Subtitle|fallback:--}\n{WindowsMedia.Title|fallback:--}");
                Add("Legacy radio metadata", "WmpLegacy.StreamName reads WM/RadioStationName from the current network media. StreamUrl is its playback URL; IsStream identifies a non-file URL. Missing station metadata remains empty: neither the title nor the URL is invented as a station name. Artist keeps its real tags. Use StreamName explicitly as a fallback variable. Install the new background plug-in matching the player architecture; older DLLs do not provide these values. Switching to a local file clears the stream fields.");
                AddExample("Example", "{WmpLegacy.Artist|fallbackvar:WmpLegacy.StreamName|fallback:--}\n{WmpLegacy.Title|fallback:--}");
                Add("Linux stream metadata", "Media.Artist and Media.Title preserve the values published by the selected MPRIS player. Tuner may publish the station name as Artist when track artist metadata is missing. LIS2 does not split combined titles or invent a station name; MPRIS has no standard station-name field. Normal music-file artist tags remain unchanged. If Artist is empty, use a literal fallback or another explicitly selected variable.");
                AddExample("Example", "{Media.Artist|fallback:--}\n{Media.Title|fallback:--}");
                Add("Linux and Windows", "Linux uses playerctl for MPRIS metadata, refreshed every second. Automatic or specific player selection is saved; an unavailable selected player stays disconnected. Windows uses WindowsMedia.* for GSMTC and Winamp.* for Winamp. GSMTC needs no plug-in. The preset picker lists only players supported by the operating system.");
                Add("Linux player audio", "Linux audio requires a running PipeWire desktop session, pipewire-bin and dbus-bin. Only a playback stream belonging to the selected MPRIS process or its children is captured; missing or ambiguous assignments show an audio error, never microphone or system audio. Metadata and audio connection states are independent. Stereo levels and twenty spectrum bands refresh about every 100 ms, including while the player is minimized. Use Media.Vu, Media.Spectrum or Media.SpectrumGlyphs, and Media.AudioConnected for visibility. Linux VU and Spectrum presets are available on Pages; Spectrum asks before replacing the eight custom glyphs. Pause, disconnection and stale audio clear levels. Diagnostics records the player, audio status and matched PipeWire node. Player matching on the actual desktop, including sandboxed players, requires a target-system test.");
                Add("Flatpak", "Player identification is automatic and does not require an installation-type setting. Native processes use host process IDs. Flatpak players use their application and instance identity to translate sandbox process IDs and D-Bus proxies. The selected audio stream remains isolated from other players. If multiple sandbox instances cannot be distinguished, audio stays disconnected with an error. Diagnostics includes the D-Bus process and sandbox identity.");
                AddExample("Media", "{Media.Player}  {Media.State}  {Media.Title}  {Media.Artist}  {Media.Album}  {Media.Elapsed}  {Media.Duration}  {Media.Connected}");
                AddExample("Example", "{Winamp.Artist|fallback:Nothing playing} / {Winamp.Title|fallback:--}");
                content.AddRange(HelpVariableCatalog.Get("Media.", language).Select(entry => (entry.Heading, entry.Body, false)));
                content.AddRange(HelpVariableCatalog.Get("Winamp.", language).Select(entry => (entry.Heading, entry.Body, false)));
                content.AddRange(HelpVariableCatalog.Get("WindowsMedia.", language).Select(entry => (entry.Heading, entry.Body, false)));
                content.AddRange(HelpVariableCatalog.Get("WmpLegacy.", language).Select(entry => (entry.Heading, entry.Body, false)));
                content.Add((UiTranslations.Translate("Windows Media Player Legacy", language),
                    UiTranslations.Translate("Register the matching x86/x64 WMP Legacy background DLL with regsvr32. Direct audio capture requires no visualization DLL.", language), false));
                content.Add((UiTranslations.Translate("Audio", language), HelpLocalization.Translate(
                    "VU and spectrum are captured directly from the Legacy player process through WASAPI. No visualization needs to be selected or visible; the player may be minimized. Windows build 20348 or later is required. Metadata and playback state still come from the background plug-in.", language), false));
                break;
            case 4:
                Add("Events", "Timed events temporarily override ordinary page output on connected displays.");
                Add("Priorities", "Critical events preempt warnings, notices and information messages. The highest priority active event is displayed.");
                Add("Lifetime", "An event expires automatically. Cancel or clear it in the Events view; ordinary page playback resumes afterward.");
                break;
            case 5:
                Add("Data freshness", SourceQualityText.FreshnessHelp);
                Add("Hardware", "The hardware browser exposes read-only sensors from the current operating system.");
Add("Windows CPU temperatures", "Windows CPU temperatures require the official signed PawnIO driver used by LibreHardwareMonitor. To run without administrator rights, explicitly grant PawnIO access to your Windows account once using tools/windows/Set-PawnIoUserAccess.ps1; installation and permission changes require elevation. This grants driver access to all programs under that account, not only LIS2 or temperature reads. Restart the application normally afterwards. Setup, verification and rollback: https://github.com/Eitschman/lis2-control-center/blob/main/docs/windows-hardware-sensors.md. Missing access can leave CPU temperatures unavailable while GPU readings still work; ACPI thermal zones are not CPU sensors.");
                Add("Sensor variables", "Use the exact Hardware.* key from the sensor browser in a page template.");
                Add("Linux and Windows", "Available temperature, utilization, fan and storage sensors depend on the host and installed drivers.");
                break;
            case 6:
                Add("Regulation", "Choose an available sensor directly or type a key, including a missing sensor. Curve points and raw text share decimal temperatures without rounding. Test tools contains simulations and preview controls. Physical authorization stays separate and is never saved.");
                Add("Fan Control", "Configure four channels, each with sensor assignment, mode and safe minimum/maximum output.");
                Add("Sensor and fan history", FanHistoryText.Help);
                Add("Regulation", "Fixed uses the configured percentage. Curve interpolates temperature=percent points and applies hysteresis. Follow rounds the sensor value to a percentage. External uses the fail-safe value because this application supplies no external control percentage. Off outputs zero only with AllowStop; otherwise it uses the minimum. Minimum and maximum limit normal outputs.");
                Add("Fail-safe", "Missing or invalid sensors in Curve and Follow use the configured fail-safe value within the channel limits. Preview and simulation do not control physical fans. Physical output requires explicit authorization for the current serial connection. Start sends 100% first; stop and orderly disconnect send 100%. A broken physical connection cannot guarantee delivery of a safety frame.");
                break;
            case 7:
                Add("Updates", UpdateText.Help);
                Add("Settings", "Select Virtual LIS2 or a serial port. Configure language, appearance and device startup preferences.");
                Add("Transport", "Applying saves the transport and port, disconnects the current device and connects the selected transport. A missing saved port remains selected and is never replaced automatically. Serial display control does not enable fans.");
                Add("Windows import", "Import in Settings reads settings.json without changing the source file. It imports transport, port, brightness, glyphs, pages, fans and sensor preferences, including Home Assistant activation and URL. The token stays separate. Reconnect Home Assistant afterward if required.");
                Add("Appearance", "The system, light and dark themes are supported. Language changes apply to the application interface.");
                Add("Settings", "Autostart uses the Windows user registry or a Linux XDG autostart entry. Windows closes to tray; Linux closes the application and keeps minimized windows in the taskbar. Tray menus offer Open and Exit. A second launch activates the existing instance. F1 opens help for the current section.");
                break;
            case 8:
                Add("Export", "Diagnostics includes version, UTC time, applied transport and port, last transmitted frame, source errors and four fan configurations. The virtual state is separate. Startup and crash traces persist in avalonia-errors.log without exception messages or tokens.");
                Add("Diagnostics", "Inspect the current device snapshot, decoded command history and protocol log.");
                Add("Export", "Export diagnostic information for troubleshooting or hardware-validation sessions.");
                Add("Virtual LIS2", "The virtual device permits protocol tests without physical hardware.");
                break;
            case 9:
                Add("Data freshness", SourceQualityText.FreshnessHelp);
                Add("Home Assistant", "The read-only WebSocket integration loads states and receives live state_changed updates.");
                Add("Connection", "Enter the Home Assistant URL and a long-lived access token, then test and save the connection.");
                Add("Token storage", "Windows uses DPAPI. Linux uses the desktop keyring through secret-tool. Local AES storage requires explicit selection; the key is stored beside the encrypted token. Keyring errors are displayed without switching storage automatically. Disabling Home Assistant stops the source and removes the token from the selected store. Switching back to the keyring migrates local files only after verifying the saved token.");
                AddExample("Entity state", "{HA.sensor.living_room_temperature}");
                Add("Home Assistant numbers", "Numeric entity states, including aliases and fallback variables, use the operating system decimal separator in templates without rounding. For example, 21.50 becomes 21,50 with German regional settings. Explicit number, percent and bytes formats control precision. Raw states, visibility expressions, text attributes and names remain unchanged; numeric JSON attributes already follow regional settings.");
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
