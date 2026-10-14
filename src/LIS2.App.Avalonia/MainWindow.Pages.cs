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
