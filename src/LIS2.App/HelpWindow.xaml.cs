using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LIS2.App;

public partial class HelpWindow : Window
{
    private readonly IReadOnlyDictionary<string, object?> _displayValues;
    private readonly IReadOnlyList<string> _glyphNames;

    private static readonly string[] SectionNames =
    [
        "Dashboard",
        "Display",
        "Pages",
        "Winamp",
        "Events",
        "Hardware",
        "Fan Control",
        "Settings",
        "Diagnostics",
        "Home Assistant"
    ];

    public HelpWindow(
        int initialSection,
        IReadOnlyDictionary<string, object?> displayValues,
        IReadOnlyList<string> glyphNames)
    {
        InitializeComponent();
        Icon = TrayIconService.CreateWindowIcon();
        SourceInitialized += (_, _) => ThemeService.ApplyWindowChrome(this);

        _displayValues = displayValues;
        _glyphNames = glyphNames;

        SectionsListBox.ItemsSource = SectionNames
            .Select(LocalizationService.Translate)
            .ToArray();

        SectionsListBox.SelectedIndex =
            Math.Clamp(initialSection, 0, SectionNames.Length - 1);
    }

    private void SectionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SectionsListBox.SelectedIndex >= 0)
            RenderSection(SectionsListBox.SelectedIndex);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void RenderSection(int index)
    {
        HelpDocument.Blocks.Clear();
        AddTitle(SectionNames[index]);

        switch (index)
        {
            case 0: RenderDashboard(); break;
            case 1: RenderDisplay(); break;
            case 2: RenderPages(); break;
            case 3: RenderWinamp(); break;
            case 4: RenderEvents(); break;
            case 5: RenderHardware(); break;
            case 6: RenderFans(); break;
            case 7: RenderSettings(); break;
            case 8: RenderDiagnostics(); break;
            case 9: RenderHomeAssistant(); break;
        }

        HelpViewer.ScrollToHome();
    }

    private void RenderDashboard()
    {
        AddParagraph("The Dashboard is the operational overview. It shows the current 20x2 VFD frame, connection state, virtual-device state, recent log messages and direct fan-output controls.");
        AddHeading("VFD Preview");
        AddParagraph("Shows the two normalized 20-character lines currently held by the application. Long page text may scroll at runtime; the preview shows the current rendered frame.");
        AddHeading("Fan Outputs");
        AddParagraph("The four sliders apply manual outputs through the same safety limits configured on Fan Control. Minimum and maximum limits still apply.");
        AddHeading("Quick links and status");
        AddParagraph("Quick links open Display or Fan Control. With Virtual transport selected, the status card mirrors the emulated LIS2 state.");
    }

    private void RenderDisplay()
    {
        AddParagraph("Display contains direct low-level controls for the physical or virtual LIS2. They bypass page scheduling and are intended for testing and one-off output.");
        AddHeading("Direct line output");
        AddParagraph("Line 1 and Line 2 are each limited to 20 display characters. Render both lines writes a complete frame. Clear sends the LIS2 clear/reset command.");
        AddHeading("Brightness");
        AddParagraph("Supported levels are 100%, 75%, 50% and 25%. Brightness-off remains intentionally unsupported until verified on real hardware.");
        AddHeading("Custom characters / CG Builder");
        AddParagraph("The LIS2 exposes eight programmable 5x8 character slots. Use these variables in page templates:");
        AddCode("{Glyph.1} ... {Glyph.8}");
        foreach (var name in _glyphNames.Where(name => !string.IsNullOrWhiteSpace(name)))
            AddCode($"{{Glyph.{NormalizeName(name)}}}");
        AddParagraph("Named glyphs use the saved glyph name after normalization.");
        AddHeading("Character ROM");
        AddParagraph("Special-character and raw-ROM tools are validation tools for the known uPD16314 mapping. Physical behavior still needs verification on the real LIS2.");
    }

    private void RenderPages()
    {
        AddParagraph("Pages define the rotating VFD presentation. Each enabled page has two templates, duration, optional visibility rules and an independent overflow mode per line.");
        AddHeading("Configuration");
        AddBullets(
            "Name: descriptive page name.",
            "Enabled: disabled pages are excluded from rotation.",
            "Duration: page lifetime in seconds; minimum 1 second.",
            "Line 1 / Line 2: text templates with {Variable} placeholders.",
            "Overflow: PingPong, Marquee or Truncate per line.",
            "Scroll step: 50–5000 ms in the editor.",
            "Edge pause: 0–10000 ms in the editor.",
            "Visible when: optional condition controlling whether the page is eligible.");
        AddHeading("Template formatting");
        AddCode("{Variable}");
        AddCode("{Variable|fallback:--}");
        AddCode("{Variable|number:1}");
        AddCode("{Variable|percent:0}");
        AddCode("{Variable|bytes:1}");
        AddCode("{Variable|prefix:CPU }");
        AddCode("{Variable|suffix:°C}");
        AddCode("{Variable|upper}");
        AddCode("{Variable|lower}");
        AddParagraph("Operations can be chained left to right. fallback handles missing, null, unknown or unavailable values. number supports 0–6 decimals. bytes uses B/KB/MB/GB/TB/PB.");
        AddCode("{HA.sensor.temperature|fallback:--|number:1|suffix:°C}");
        AddCode("{HA.sensor.disk_free|fallback:--|bytes:1}");
        AddHeading("Visibility expressions");
        AddParagraph("Supported operators are =, !=, >, >=, < and <=. Equality is case-insensitive text comparison; ordering operators require numeric values.");
        AddCode("Winamp.State=Playing");
        AddCode("HA.sensor.temperature>=25");
        AddHeading("Clock variables");
        AddVariable("Clock.Time", "Current local time, HH:mm:ss.");
        AddVariable("Clock.Date", "Current local date, dd.MM.yyyy.");
        AddVariable("Clock.Day", "Current local weekday.");
        AddHeading("Currently available variables");
        AddVariableList(_displayValues.Keys);
        AddParagraph("This list comes from the current runtime snapshot. Home Assistant itself is intentionally not expanded into thousands of help entries; use the dedicated HA help with examples and the HA browser for exact entity/attribute names.");
    }

    private void RenderWinamp()
    {
        AddParagraph("Winamp telemetry comes from the native x86 gen_lis2.dll plug-in over the LIS2ControlCenter.Winamp named pipe.");
        AddHeading("All Winamp variables");
        AddVariable("Winamp.State", "Unknown, Stopped, Paused or Playing.");
        AddVariable("Winamp.Artist", "Current artist.");
        AddVariable("Winamp.Title", "Current title.");
        AddVariable("Winamp.Album", "Current album.");
        AddVariable("Winamp.PlaylistPosition", "Current playlist position.");
        AddVariable("Winamp.PlaylistCount", "Number of playlist entries.");
        AddVariable("Winamp.Elapsed", "Elapsed time as mm:ss.");
        AddVariable("Winamp.Duration", "Track duration as mm:ss.");
        AddVariable("Winamp.BitrateKbps", "Bitrate in kbit/s.");
        AddVariable("Winamp.SampleRateHz", "Sample rate in Hz.");
        AddVariable("Winamp.VuLeft", "Left VU level.");
        AddVariable("Winamp.VuRight", "Right VU level.");
        AddVariable("Winamp.Vu", "Preformatted 20-character stereo VU line.");
        AddVariable("Winamp.Spectrum", "20-band ASCII spectrum.");
        AddVariable("Winamp.SpectrumRaw", "Raw spectrum array.");
        AddVariable("Winamp.SpectrumPeak", "Peak spectrum value.");
        AddVariable("Winamp.SpectrumGlyphs", "20-band spectrum using the eight custom VFD glyph levels.");
        AddVariable("Winamp.Connected", "True while snapshots are fresh.");
        AddVariable("Winamp.SnapshotAgeSeconds", "Age of the last snapshot.");
        AddHeading("Typical page");
        AddCode("{Winamp.Artist|fallback:Nothing playing}");
        AddCode("{Winamp.Title|fallback:--}");
        AddCode("Visible when: Winamp.State=Playing");
    }

    private void RenderEvents()
    {
        AddParagraph("Events are temporary priority overlays. While active they override normal page rotation; after expiry the scheduler returns to the normal page.");
        AddBullets(
            "Line 1 / Line 2: event text.",
            "Priority: higher priority events win.",
            "Duration: 1–3600 seconds.",
            "Expired events are removed automatically.");
    }

    private void RenderHardware()
    {
        AddParagraph("Hardware uses LibreHardwareMonitor and publishes CPU, GPU, memory, motherboard, storage, network and controller sensor values.");
        AddHeading("Variable pattern");
        AddCode("{Hardware.<hardware>.<sensor-type>.<sensor>}");
        AddCode("{Hardware.<hardware>.<sensor-type>.<sensor>.Unit}");
        AddParagraph("Hardware and sensor names are normalized: non-alphanumeric characters become underscores. Exact variable names depend on the computer.");
        AddHeading("Units");
        AddBullets("Temperature → C", "Load → %", "Fan → RPM", "Clock → MHz", "Voltage → V", "Power → W", "Data → GB", "SmallData → MB", "Throughput → B/s");
        AddHeading("Currently detected hardware variables");
        AddVariableList(_displayValues.Keys.Where(key => key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase)));
    }

    private void RenderFans()
    {
        AddParagraph("Fan Control configures the four LIS2 fan channels. Software safety limits are applied before output is sent.");
        AddHeading("Modes");
        AddBullets(
            "Fixed: configured fixed output.",
            "Curve: interpolates between temperature/output points.",
            "Follow: converts the selected numeric sensor value directly to a percentage.",
            "External: uses an externally supplied percentage.",
            "Off: outputs 0% only when Allow stop is enabled; otherwise minimum output is used.");
        AddHeading("Safety");
        AddBullets(
            "Minimum: lower non-zero limit.",
            "Maximum: upper limit.",
            "Fail-safe: used when a required sensor is missing or invalid.",
            "Hysteresis: suppresses small curve changes.",
            "Allow stop: permits 0% and requires explicit confirmation.");
        AddParagraph("Safe minimum speed and reliable restart at 0% are physical properties and must be verified with the actual fan hardware.");
    }

    private void RenderSettings()
    {
        AddParagraph("Settings controls transport, COM port, appearance/language and Windows startup.");
        AddHeading("Transport");
        AddBullets(
            "Virtual: software-only emulator and recommended mode without hardware.",
            "Serial: physical LIS2. A COM port is required.",
            "Known serial parameters: 19200 baud, 8 data bits, no parity, one stop bit.");
        AddHeading("Appearance");
        AddParagraph("Theme can follow Windows or use Light/Dark. Language can follow the system or use English, German, French, Turkish or Russian.");
        AddHeading("Startup / tray");
        AddParagraph("Autostart is per-user. Closing the main window normally keeps the application alive in the tray.");
    }

    private void RenderDiagnostics()
    {
        AddParagraph("Diagnostics combines application state, source health, Virtual LIS2 state and decoded command history.");
        AddHeading("Virtual LIS2");
        AddParagraph("Shows both lines, brightness, four fan outputs, all eight glyphs, rejected-command count and the most recent protocol error.");
        AddHeading("Command history");
        AddParagraph("Shows up to the last 200 writes as hexadecimal bytes plus decoded command descriptions.");
        AddHeading("Export");
        AddParagraph("Export diagnostic report saves the current snapshot for bug reports and hardware-validation sessions.");
    }

    private void RenderHomeAssistant()
    {
        AddParagraph("Home Assistant is the universal read-only integration layer. The app authenticates to the WebSocket API, loads the initial states and then receives live state_changed events.");
        AddHeading("Connection");
        AddBullets(
            "URL: absolute http:// or https:// Home Assistant base URL.",
            "Token: long-lived Home Assistant access token.",
            "Test connection: checks WebSocket greeting and authentication.",
            "Save / reconnect: stores settings and restarts the HA source.",
            "Connection failures are retried automatically.");
        AddHeading("The four basic variable forms");
        AddCode("{HA.sensor.living_room_temperature}");
        AddParagraph("Entity state. Example result: 21.7");
        AddCode("{HA.sensor.living_room_temperature.Unit}");
        AddParagraph("unit_of_measurement. Example result: °C");
        AddCode("{HA.sensor.living_room_temperature.FriendlyName}");
        AddParagraph("friendly_name. Example result: Living room temperature");
        AddCode("{HA.sensor.living_room_temperature.attribute.device_class}");
        AddParagraph("Any attribute exposed by the entity. Example result: temperature");

        AddHeading("Attribute examples");
        AddParagraph("The exact attributes depend entirely on the Home Assistant integration. Select an entity in the HA browser to see every attribute currently exposed by that entity.");
        AddCode("{HA.media_player.living_room.attribute.media_title}");
        AddParagraph("Example: current media title, if the media_player exposes media_title.");
        AddCode("{HA.media_player.living_room.attribute.media_artist}");
        AddParagraph("Example: current media artist, if available.");
        AddCode("{HA.sensor.server_disk.attribute.free}");
        AddParagraph("Example: a custom/integration-specific disk attribute named free.");
        AddCode("{HA.weather.home.attribute.temperature}");
        AddParagraph("Example: an attribute named temperature, if that weather entity exposes it.");
        AddParagraph("These are syntax examples, not a promise that every HA installation has those exact entities or attributes.");

        AddHeading("Aliases");
        AddParagraph("Assign an alias in the HA browser when an entity ID is long or unstable. The same state, Unit, FriendlyName and attribute namespace is then available below the alias.");
        AddCode("{HA.LivingRoomTemp}");
        AddCode("{HA.LivingRoomTemp.Unit}");
        AddCode("{HA.LivingRoomTemp.FriendlyName}");
        AddCode("{HA.LivingRoomTemp.attribute.device_class}");

        AddHeading("Formatting examples");
        AddCode("{HA.sensor.living_room_temperature|fallback:--|number:1|suffix:°C}");
        AddCode("{HA.sensor.server_disk_used|fallback:--|percent:0}");
        AddCode("{HA.sensor.server_disk_free_bytes|fallback:--|bytes:1}");
        AddCode("{HA.media_player.living_room.attribute.media_title|fallback:Nothing playing}");
        AddParagraph("Formatting operations are the same as on every page variable: fallback, number, percent, bytes, prefix, suffix, upper and lower.");

        AddHeading("Visibility examples");
        AddCode("HA.binary_sensor.window=on");
        AddCode("HA.sensor.living_room_temperature>=25");
        AddCode("HA.media_player.living_room=playing");
        AddParagraph("Use the Visibility Builder on Pages to select a live HA variable and operator instead of typing the expression manually.");

        AddHeading("unknown / unavailable");
        AddParagraph("The template renderer treats null, unknown and unavailable as unavailable. This is why fallback is especially useful for HA values.");
        AddCode("{HA.sensor.outdoor_temperature|fallback:--}");

        AddHeading("Arrays and objects");
        AddParagraph("Some integrations expose structured attributes such as lists or objects. LIS2 Control Center exposes them as compact JSON.");
        AddCode("{HA.some_entity.attribute.some_list}");
        AddParagraph("Example result: [1,2,3]");
        AddCode("{HA.some_entity.attribute.some_object}");
        AddParagraph("Example result: {\"temperature\":21,\"condition\":\"sunny\"}");
        AddParagraph("For a 20x2 VFD, structured JSON is usually too large to display directly. Use the HA browser to inspect it; dedicated extraction/formatting should only be added for a concrete real-world use case.");

        AddHeading("How to find the exact variable");
        AddBullets(
            "Open Home Assistant in LIS2 Control Center.",
            "Search by entity ID, friendly name or alias.",
            "Select the entity.",
            "Use the attribute list to inspect every available attribute.",
            "Use Copy key or Create display page to avoid typing long keys manually.");
        AddParagraph("The help deliberately does not list every entity from your real Home Assistant instance. Large installations can have thousands of entities; the HA browser is the authoritative live catalogue.");
    }

    private void AddTitle(string text) =>
        HelpDocument.Blocks.Add(new Paragraph(new Run(LocalizationService.Translate(text)))
        {
            FontSize = 26,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 14)
        });

    private void AddHeading(string text) =>
        HelpDocument.Blocks.Add(new Paragraph(new Run(text))
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 18, 0, 6)
        });

    private void AddParagraph(string text) =>
        HelpDocument.Blocks.Add(new Paragraph(new Run(text))
        {
            Margin = new Thickness(0, 0, 0, 8),
            LineHeight = 21
        });

    private void AddCode(string text) =>
        HelpDocument.Blocks.Add(new Paragraph(new Run(text))
        {
            FontFamily = new FontFamily("Consolas"),
            Background = (Brush)FindResource("InputBrush"),
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(0, 3, 0, 5)
        });

    private void AddVariable(string key, string description)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 2, 0, 5) };
        paragraph.Inlines.Add(new Run($"{{{key}}}") { FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.SemiBold });
        paragraph.Inlines.Add(new Run($" — {description}"));
        HelpDocument.Blocks.Add(paragraph);
    }

    private void AddVariableList(IEnumerable<string> keys)
    {
        var ordered = keys
            .Where(key => !key.StartsWith("HA.", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ordered.Length == 0)
        {
            AddParagraph("No matching runtime variables are currently available.");
            return;
        }

        foreach (var key in ordered)
            AddCode($"{{{key}}}");
    }

    private void AddBullets(params string[] items)
    {
        var list = new List { MarkerStyle = TextMarkerStyle.Disc, Margin = new Thickness(20, 2, 0, 8) };
        foreach (var item in items)
            list.ListItems.Add(new ListItem(new Paragraph(new Run(item)) { Margin = new Thickness(0, 1, 0, 3) }));
        HelpDocument.Blocks.Add(list);
    }

    private static string NormalizeName(string value)
    {
        var chars = value
            .Select(character => char.IsLetterOrDigit(character) || character is '_' or '-'
                ? character
                : '_')
            .ToArray();

        return new string(chars).Trim('_');
    }
}
