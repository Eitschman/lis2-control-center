using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Globalization;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PageWorkspace _pages = new();
    private readonly DisplayRuntimeHost _runtime = new();
    private PageSourceValues _pageSources => _runtime.Sources;
    private bool _loadingPage;
    private int? _loadedPageIndex;
    private bool _handlingPageSelection;
    private readonly PageEditorPreview _editorPreview = new();
    private readonly DispatcherTimer _editorPreviewTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private bool _editorPreviewConfigured;

    private void InitializePagesEditor()
    {
        _editorPreviewTimer.Tick += (_, _) => TickEditorPreview();
        Closed += (_, _) => _editorPreviewTimer.Stop();
        this.FindControl<ListBox>("PagesList")!.SelectionChanged += async (_, _) => await HandlePageSelectionAsync();
        this.FindControl<Button>("PageCreatePreset")!.Click += async (_, _) =>
        {
            try
            {
                var choice = this.FindControl<ComboBox>("PagePresetPicker")!.SelectedIndex;
                var preset = CreateWpfPagePreset(choice);
                if (choice == 9 && !await EnsureSpectrumGlyphsAsync()) return;
                if (!CommitPendingPageEditor()) return;
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
                await ReloadRunningPagesAsync();
                _ = SavePortableWorkspaceAsync();
            }
            catch (Exception ex)
            {
                SetVirtualStatus("Page preset error: " + ex.Message);
            }
        };
        this.FindControl<Button>("PageAdd")!.Click += async (_, _) =>
        {
            if (!CommitPendingPageEditor()) return;
            _pages.Add();
            RefreshPageList(_pages.Pages.Count - 1);
            await ReloadRunningPagesAsync();
            _ = SavePortableWorkspaceAsync();
        };
        this.FindControl<Button>("PageDuplicate")!.Click += async (_, _) =>
        {
            if (SelectedPageIndex is int i)
            {
                if (!CommitPendingPageEditor()) return;
                _pages.Duplicate(i);
                RefreshPageList(i + 1);
                await ReloadRunningPagesAsync();
                _ = SavePortableWorkspaceAsync();
            }
        };
        this.FindControl<Button>("PageDelete")!.Click += async (_, _) =>
        {
            if (SelectedPageIndex is int i)
            {
                if (!CommitPendingPageEditor()) return;
                _pages.Delete(i);
                RefreshPageList(Math.Min(i, _pages.Pages.Count - 1));
                await ReloadRunningPagesAsync();
                _ = SavePortableWorkspaceAsync();
            }
        };
        this.FindControl<Button>("PageUp")!.Click += async (_, _) => await MovePageAsync(-1);
        this.FindControl<Button>("PageDown")!.Click += async (_, _) => await MovePageAsync(1);
        this.FindControl<Button>("PageSave")!.Click += async (_, _) =>
        {
            if (SavePageEditor()) await ReloadRunningPagesAsync();
        };
        this.FindControl<Button>("PageTest")!.Click += async (_, _) =>
        {
            if (SelectedPageIndex is not int i) return;
            if (!SavePageEditor()) return;
            var frame = _pages.Preview(i, PagePreviewValues());
            await RunVirtualActionAsync(() => _virtualDevice.RenderBothAsync(frame.Line1, frame.Line2));
        };
        foreach (var name in new[] { "PageLine1", "PageLine2", "PageDuration", "PageRefresh", "PageScrollStep", "PageScrollPause" })
            this.FindControl<TextBox>(name)!.TextChanged += (_, _) => RefreshPagePreview();
        foreach (var name in new[] { "PageOverflow1", "PageOverflow2" })
            this.FindControl<ComboBox>(name)!.SelectionChanged += (_, _) => RefreshPagePreview();
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


    private async Task<bool> EnsureSpectrumGlyphsAsync()
    {
        if (BuiltInGlyphSets.IsSpectrumSet(_glyphs.Glyphs))
            return true;

        var dialog = new Window
        {
            Title = BrowserText("Replace custom glyphs?"),
            Icon = Icon,
            Width = 550,
            Height = 210,
            MinWidth = 450,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var message = new TextBlock
        {
            Text = BrowserText("The VFD spectrum uses all eight custom character slots. Replace the current glyph set with the eight spectrum bar levels?"),
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
        };
        var confirm = new Button { Content = BrowserText("Replace glyphs") };
        var cancel = new Button { Content = BrowserText("Cancel") };
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };
        actions.Children.Add(cancel);
        actions.Children.Add(confirm);
        var content = new StackPanel { Margin = new global::Avalonia.Thickness(20), Spacing = 18 };
        content.Children.Add(message);
        content.Children.Add(actions);
        dialog.Content = content;
        confirm.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);
        if (!await dialog.ShowDialog<bool>(this))
            return false;

        _glyphs.Replace(BuiltInGlyphSets.CreateSpectrumSet());
        LoadGlyphEditor();
        await SavePortableWorkspaceAsync();
        if (_virtualDevice.IsConnected)
            await _virtualDevice.ProgramAllGlyphsAsync(_glyphs.Glyphs);
        return true;
    }

    private PagePresetSpec CreateWpfPagePreset(int choice)
    {
        return choice switch
        {
            0 => PagePresetCatalog.Get(0),
            1 => PagePresetCatalog.Get(1),
            2 => CreateHardwarePagePreset("CPU", "Load", "Temperature", "CPU", "Temp", "CPU", "Processor"),
            3 => CreateHardwarePagePreset("GPU", "Load", "Temperature", "GPU", "Temp", "GPU", "Graphics"),
            4 => CreateHardwarePagePreset("Memory", "Load", "Data", "RAM", "Used", "Memory", "RAM"),
            5 => CreateHardwarePagePreset("Temperatures", "Temperature", "Temperature", "T1", "T2"),
            6 => CreateHardwarePagePreset("Fans", "Fan", "Fan", "Fan1", "Fan2"),
            7 or 8 or 9 or 10 => PagePresetCatalog.Get(choice),
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

    private sealed record PageListRow(int Index, string Name, bool Enabled);

    private async void PageListEnabledClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_loadingPage || sender is not CheckBox { DataContext: PageListRow row } checkbox)
            return;

        if (row.Index < 0 || row.Index >= _pages.Pages.Count)
            return;

        if (!CommitPendingPageEditor())
        {
            // Restore the checkbox from the unchanged model when the current
            // editor contains invalid data.
            _loadingPage = true;
            checkbox.IsChecked = _pages.Get(row.Index).Enabled;
            _loadingPage = false;
            return;
        }
        _pages.Get(row.Index).Enabled = checkbox.IsChecked == true;
        var selectedIndex = SelectedPageIndex ?? row.Index;
        RefreshPageList(selectedIndex);
        await ReloadRunningPagesAsync();
        await SavePortableWorkspaceAsync();
    }

    private bool CommitPendingPageEditor()
    {
        return _loadedPageIndex is not int index ||
            index < 0 || index >= _pages.Pages.Count ||
            SavePageEditor(index, refreshEditor: false);
    }

    private async Task HandlePageSelectionAsync()
    {
        if (_loadingPage || _handlingPageSelection) return;
        var next = SelectedPageIndex;
        if (_loadedPageIndex == next) return;
        _handlingPageSelection = true;
        try
        {
            if (_loadedPageIndex is int previous && previous >= 0 && previous < _pages.Pages.Count)
            {
                if (!CommitPendingPageEditor())
                {
                    // Invalid edits must not disappear when the user selects
                    // another page. Keep the editor and selection together.
                    _loadingPage = true;
                    this.FindControl<ListBox>("PagesList")!.SelectedIndex = previous;
                    _loadingPage = false;
                    return;
                }
                await ReloadRunningPagesAsync();
            }
            LoadPageEditor();
        }
        finally { _handlingPageSelection = false; }
    }

    private int? SelectedPageIndex =>
        this.FindControl<ListBox>("PagesList")!.SelectedIndex is int i && i >= 0 && i < _pages.Pages.Count ? i : null;

    private void RefreshPageList(int selection)
    {
        _loadingPage = true;
        var list = this.FindControl<ListBox>("PagesList")!;
        list.ItemsSource = null;
        list.ItemsSource = _pages.Pages.Select((p, index) => new PageListRow(index, p.Name, p.Enabled)).ToArray();
        list.SelectedIndex = selection;
        _loadingPage = false;
        _loadedPageIndex = SelectedPageIndex;
        if (SelectedPageIndex is null)
        {
            ClearPageEditorPreview();
            return;
        }
        LoadPageEditor();
    }

    private void ClearPageEditorPreview()
    {
        _editorPreviewTimer.Stop();
        _editorPreviewConfigured = false;
        this.FindControl<TextBlock>("PagePreview1")!.Text = string.Empty;
        this.FindControl<TextBlock>("PagePreview2")!.Text = string.Empty;
    }

    private async Task MovePageAsync(int offset)
    {
        if (SelectedPageIndex is not int i) return;
        if (!CommitPendingPageEditor()) return;
        RefreshPageList(_pages.Move(i, offset));
        await ReloadRunningPagesAsync();
        _ = SavePortableWorkspaceAsync();
    }

    private void LoadPageEditor()
    {
        if (_loadingPage) return;
        if (SelectedPageIndex is not int i)
        {
            _loadedPageIndex = null;
            ClearPageEditorPreview();
            return;
        }
        _loadedPageIndex = i;
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
        UpdateConditionStatus();
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
        if (_loadingPage) return;
        if (SelectedPageIndex is not int i)
        {
            ClearPageEditorPreview();
            return;
        }
        var original = _pages.Get(i);
        var line1 = this.FindControl<TextBox>("PageLine1")!.Text ?? original.Line1Template;
        var line2 = this.FindControl<TextBox>("PageLine2")!.Text ?? original.Line2Template;
        var overflow1 = OverflowName(this.FindControl<ComboBox>("PageOverflow1")!.SelectedIndex);
        var overflow2 = OverflowName(this.FindControl<ComboBox>("PageOverflow2")!.SelectedIndex);
        var refreshText = this.FindControl<TextBox>("PageRefresh")!.Text;
        var refresh = int.TryParse(refreshText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRefresh)
            && parsedRefresh > 0 ? parsedRefresh : original.RefreshIntervalMilliseconds;
        var stepText = this.FindControl<TextBox>("PageScrollStep")!.Text;
        var pauseText = this.FindControl<TextBox>("PageScrollPause")!.Text;
        var step = int.TryParse(stepText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStep)
            && parsedStep > 0 ? parsedStep : original.ScrollStepMilliseconds;
        var pause = int.TryParse(pauseText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPause)
            && parsedPause >= 0 ? parsedPause : original.ScrollEdgePauseMilliseconds;
        _editorPreview.Configure(line1, line2, overflow1, overflow2, step, pause, refresh);
        _editorPreviewConfigured = true;
        UpdateEditorPreviewVisibility();
    }

    private void UpdateEditorPreviewVisibility()
    {
        if (_selectedSection != 2 || !_editorPreviewConfigured || SelectedPageIndex is null)
        {
            _editorPreviewTimer.Stop();
            return;
        }
        if (!_editorPreviewTimer.IsEnabled) _editorPreviewTimer.Start();
        TickEditorPreview();
    }

    private void TickEditorPreview()
    {
        if (_selectedSection != 2 || !_editorPreviewConfigured || _loadingPage || SelectedPageIndex is null)
        {
            _editorPreviewTimer.Stop();
            return;
        }
        var frame = _editorPreview.Tick(DateTimeOffset.UtcNow, PagePreviewValues());
        this.FindControl<TextBlock>("PagePreview1")!.Text = frame.Line1;
        this.FindControl<TextBlock>("PagePreview2")!.Text = frame.Line2;
        var delay = _editorPreview.NextDelay;
        _editorPreviewTimer.Interval = delay < TimeSpan.FromMilliseconds(20)
            ? TimeSpan.FromMilliseconds(20) : delay;
    }

    private bool SavePageEditor() => SelectedPageIndex is int index && SavePageEditor(index);

    private bool SavePageEditor(int i, bool refreshEditor = true)
    {
        if (i < 0 || i >= _pages.Pages.Count) return false;
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
            if (refreshEditor) RefreshPageList(i);
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
