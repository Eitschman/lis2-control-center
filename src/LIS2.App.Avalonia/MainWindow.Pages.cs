using Avalonia.Controls;
using System.Globalization;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PageWorkspace _pages = new();
    private readonly DisplayRuntimeHost _runtime = new();
    private PageSourceValues _pageSources => _runtime.Sources;
    private bool _loadingPage;

    private void InitializePagesEditor()
    {
        this.FindControl<ListBox>("PagesList")!.SelectionChanged += (_, _) => LoadPageEditor();
        this.FindControl<Button>("PageCreatePreset")!.Click += (_, _) =>
        {
            try
            {
                var choice = this.FindControl<ComboBox>("PagePresetPicker")!.SelectedIndex;
                var preset = CreateWpfPagePreset(choice);
                _pages.Add();
                var index = _pages.Pages.Count - 1;
                var page = _pages.Get(index);
                page.Name = preset.Name;
                page.Line1Template = preset.Line1;
                page.Line2Template = preset.Line2;
                page.DurationSeconds = preset.Duration;
                page.VisibilityExpression = preset.Visibility;
                page.Line1OverflowMode = preset.Overflow1;
                page.Line2OverflowMode = preset.Overflow2;
                page.ScrollStepMilliseconds = preset.ScrollStep;
                page.ScrollEdgePauseMilliseconds = preset.EdgePause;
                RefreshPageList(index);
                _ = SavePortableWorkspaceAsync();
            }
            catch (Exception ex)
            {
                SetVirtualStatus("Page preset error: " + ex.Message);
            }
        };
        this.FindControl<Button>("PageAdd")!.Click += (_, _) =>
        {
            _pages.Add();
            RefreshPageList(_pages.Pages.Count - 1);
            _ = SavePortableWorkspaceAsync();
        };
        this.FindControl<Button>("PageDuplicate")!.Click += (_, _) =>
        {
            if (SelectedPageIndex is int i)
            {
                _pages.Duplicate(i);
                RefreshPageList(i + 1);
                _ = SavePortableWorkspaceAsync();
            }
        };
        this.FindControl<Button>("PageDelete")!.Click += (_, _) =>
        {
            if (SelectedPageIndex is int i)
            {
                _pages.Delete(i);
                RefreshPageList(Math.Min(i, _pages.Pages.Count - 1));
                _ = SavePortableWorkspaceAsync();
            }
        };
        this.FindControl<Button>("PageUp")!.Click += (_, _) => MovePage(-1);
        this.FindControl<Button>("PageDown")!.Click += (_, _) => MovePage(1);
        this.FindControl<Button>("PageSave")!.Click += (_, _) => SavePageEditor();
        this.FindControl<Button>("PageTest")!.Click += async (_, _) =>
        {
            if (SelectedPageIndex is not int i) return;
            if (!SavePageEditor()) return;
            var frame = _pages.Preview(i, PagePreviewValues());
            await RunVirtualActionAsync(() => _virtualDevice.RenderBothAsync(frame.Line1, frame.Line2));
        };
        foreach (var name in new[] { "PageLine1", "PageLine2" })
            this.FindControl<TextBox>(name)!.TextChanged += (_, _) => RefreshPagePreview();
        InitializePageTools();
        _pages.Add();
        RefreshPageList(0);
        InitializePagePlayback();
        InitializePageVariables();
        InitializeHomeAssistant();
        LoadPortableWorkspace();
        InitializeWindowsSettingsImport();
        _ = StartPageSourcesAsync();
    }

    private sealed record PagePresetSpec(
        string Name, string Line1, string Line2, int Duration = 5,
        string Visibility = "", string Overflow1 = "Truncate",
        string Overflow2 = "Truncate", int ScrollStep = 250, int EdgePause = 800);

    private PagePresetSpec CreateWpfPagePreset(int choice)
    {
        return choice switch
        {
            0 => new("Clock", "{Clock.Time}", "{Clock.Date}"),
            1 => new("LIS2 Status", "LIS2 Control Center", "Ready"),
            2 => CreateHardwarePagePreset("CPU", "Load", "Temperature", "CPU", "Temp", "CPU", "Processor"),
            3 => CreateHardwarePagePreset("GPU", "Load", "Temperature", "GPU", "Temp", "GPU", "Graphics"),
            4 => CreateHardwarePagePreset("Memory", "Load", "Data", "RAM", "Used", "Memory", "RAM"),
            5 => CreateHardwarePagePreset("Temperatures", "Temperature", "Temperature", "T1", "T2"),
            6 => CreateHardwarePagePreset("Fans", "Fan", "Fan", "Fan1", "Fan2"),
            7 => new("Winamp Now Playing", "{Winamp.Artist}", "{Winamp.Title}", 7,
                "Winamp.State=Playing", "PingPong", "PingPong"),
            8 => new("Winamp VU", "{Winamp.Title}", "{Winamp.Vu}", 7,
                "Winamp.State=Playing", "PingPong"),
            9 => new("Winamp Spectrum", "{Winamp.Artist}", "{Winamp.SpectrumGlyphs}", 7,
                "Winamp.State=Playing", "PingPong"),
            10 => new("Windows Media Now Playing", "{WindowsMedia.Artist|fallback:--}",
                "{WindowsMedia.Title|fallback:--}", 7,
                "WindowsMedia.State=Playing", "PingPong", "PingPong"),
            _ => throw new ArgumentOutOfRangeException(nameof(choice), "Unknown page preset.")
        };
    }

    private PagePresetSpec CreateHardwarePagePreset(
        string name, string type1, string type2, string label1, string label2,
        params string[] matchingTerms)
    {
        var values = _runtime.Snapshot();
        var sensors = values.Keys
            .Where(key => key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase)
                && !key.EndsWith(".Unit", StringComparison.OrdinalIgnoreCase)
                && values.TryGetValue(key, out var value)
                && value is not null
                && double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] Matching(string type) => sensors.Where(key =>
            key.Contains(type, StringComparison.OrdinalIgnoreCase) &&
            (matchingTerms.Length == 0 || matchingTerms.Any(term =>
                key.Contains(term, StringComparison.OrdinalIgnoreCase)))).ToArray();

        var first = Matching(type1).FirstOrDefault();
        var second = Matching(type2).FirstOrDefault(key => key != first);
        if (first is null && second is null)
            throw new InvalidOperationException($"No matching hardware sensors for '{name}'.");

        string Line(string label, string? key) => key is null ? "" :
            label + " {" + key + "}" +
            (values.TryGetValue(key + ".Unit", out var unit) ? Convert.ToString(unit) : "");

        return new PagePresetSpec(name,
            first is null ? name : Line(label1, first),
            Line(label2, second), Overflow1: "PingPong", Overflow2: "PingPong");
    }

    private int? SelectedPageIndex =>
        this.FindControl<ListBox>("PagesList")!.SelectedIndex is int i && i >= 0 && i < _pages.Pages.Count ? i : null;

    private void RefreshPageList(int selection)
    {
        _loadingPage = true;
        var list = this.FindControl<ListBox>("PagesList")!;
        list.ItemsSource = null;
        list.ItemsSource = _pages.Pages.Select(p => p.Enabled ? p.Name : $"(off) {p.Name}").ToArray();
        list.SelectedIndex = selection;
        _loadingPage = false;
        LoadPageEditor();
    }

    private void MovePage(int offset)
    {
        if (SelectedPageIndex is not int i) return;
        RefreshPageList(_pages.Move(i, offset));
        _ = SavePortableWorkspaceAsync();
    }

    private void LoadPageEditor()
    {
        if (_loadingPage || SelectedPageIndex is not int i) return;
        var p = _pages.Get(i);
        _loadingPage = true;
        this.FindControl<TextBox>("PageName")!.Text = p.Name;
        this.FindControl<TextBox>("PageLine1")!.Text = p.Line1Template;
        this.FindControl<TextBox>("PageLine2")!.Text = p.Line2Template;
        this.FindControl<TextBox>("PageDuration")!.Text = p.DurationSeconds.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("PageRefresh")!.Text = p.RefreshIntervalMilliseconds.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("PageVisibility")!.Text = p.VisibilityExpression;
        this.FindControl<ComboBox>("PageOverflow1")!.SelectedIndex = OverflowIndex(p.Line1OverflowMode);
        this.FindControl<ComboBox>("PageOverflow2")!.SelectedIndex = OverflowIndex(p.Line2OverflowMode);
        this.FindControl<TextBox>("PageScrollStep")!.Text = p.ScrollStepMilliseconds.ToString(CultureInfo.InvariantCulture);
        this.FindControl<TextBox>("PageScrollPause")!.Text = p.ScrollEdgePauseMilliseconds.ToString(CultureInfo.InvariantCulture);
        this.FindControl<CheckBox>("PageEnabled")!.IsChecked = p.Enabled;
        _loadingPage = false;
        RefreshPagePreview();
    }

    private static int OverflowIndex(string mode) => mode switch
    {
        "Truncate" => 1, "Marquee" => 2, _ => 0
    };

    private static string OverflowName(int index) => index switch
    {
        1 => "Truncate", 2 => "Marquee", _ => "PingPong"
    };

    private IReadOnlyDictionary<string, object?> PagePreviewValues()
    {
        return _runtime.Snapshot();
    }

    private async Task StartPageSourcesAsync()
    {
        try
        {
            await _runtime.StartSourcesAsync();
            // Workspace credentials are restored before this startup task runs.
            // Configure the actual HA source, not only the URL/token controls.
            if (this.FindControl<CheckBox>("HaEnabled")!.IsChecked == true)
            {
                var url = this.FindControl<TextBox>("HaUrl")!.Text;
                var token = this.FindControl<TextBox>("HaToken")!.Text;
                if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(token))
                {
                    try
                    {
                        await _pageSources.ConfigureHomeAssistantAsync(url, token);
                    }
                    catch (Exception ex)
                    {
                        this.FindControl<TextBlock>("HaStatus")!.Text = $"Home Assistant startup failed: {ex.Message}";
                    }
                }
            }
            RefreshHaEntities();
            RefreshPageVariables();
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Clock source startup failed: {ex.Message}");
        }
    }

    private void RefreshPagePreview()
    {
        if (_loadingPage || SelectedPageIndex is not int i) return;
        var original = _pages.Get(i);
        var frame = new LIS2.Display.TemplateRenderer().RenderFrame(
            this.FindControl<TextBox>("PageLine1")!.Text ?? original.Line1Template,
            this.FindControl<TextBox>("PageLine2")!.Text ?? original.Line2Template,
            PagePreviewValues());
        this.FindControl<TextBlock>("PagePreview1")!.Text = frame.Line1;
        this.FindControl<TextBlock>("PagePreview2")!.Text = frame.Line2;
    }

    private bool SavePageEditor()
    {
        if (SelectedPageIndex is not int i) return false;
        try
        {
            var duration = int.Parse(this.FindControl<TextBox>("PageDuration")!.Text ?? "", CultureInfo.InvariantCulture);
            var refresh = int.Parse(this.FindControl<TextBox>("PageRefresh")!.Text ?? "", CultureInfo.InvariantCulture);
            var step = int.Parse(this.FindControl<TextBox>("PageScrollStep")!.Text ?? "", CultureInfo.InvariantCulture);
            var pause = int.Parse(this.FindControl<TextBox>("PageScrollPause")!.Text ?? "", CultureInfo.InvariantCulture);
            if (duration <= 0 || refresh is < 100 or > 60000 || step <= 0 || pause < 0)
                throw new ArgumentOutOfRangeException(nameof(duration), "Invalid page timing values.");
            var visibility = this.FindControl<TextBox>("PageVisibility")!.Text;
            if (!string.IsNullOrWhiteSpace(visibility) &&
                !LIS2.Display.VisibilityEvaluator.TryParse(visibility, out _, out _, out _))
                throw new InvalidOperationException("Invalid visibility expression.");
            var p = _pages.Get(i);
            p.Name = this.FindControl<TextBox>("PageName")!.Text ?? "";
            p.Line1Template = this.FindControl<TextBox>("PageLine1")!.Text ?? "";
            p.Line2Template = this.FindControl<TextBox>("PageLine2")!.Text ?? "";
            p.DurationSeconds = duration;
            p.RefreshIntervalMilliseconds = refresh;
            p.ScrollStepMilliseconds = step;
            p.ScrollEdgePauseMilliseconds = pause;
            p.VisibilityExpression = visibility;
            p.Line1OverflowMode = OverflowName(this.FindControl<ComboBox>("PageOverflow1")!.SelectedIndex);
            p.Line2OverflowMode = OverflowName(this.FindControl<ComboBox>("PageOverflow2")!.SelectedIndex);
            p.Enabled = this.FindControl<CheckBox>("PageEnabled")!.IsChecked == true;
            RefreshPageList(i);
            SetVirtualStatus($"Saved page '{p.Name}' in editor workspace.");
            _ = SavePortableWorkspaceAsync();
            return true;
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Page validation error: {ex.Message}");
            return false;
        }
    }
}
