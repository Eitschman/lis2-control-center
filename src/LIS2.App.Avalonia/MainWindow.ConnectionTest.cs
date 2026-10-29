using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private sealed record ConnectionSource(string Id, string Label) { public override string ToString() => Label; }
    private CancellationTokenSource? _connectionTestStop;
    private Task _connectionTestTask = Task.CompletedTask;
    private IReadOnlyList<ConnectionTestResult> _connectionTestResults = [];
    private string? _connectionTestSource;
    private DateTimeOffset? _connectionTestAt;
    private bool _connectionSourceRebinding;

    private void InitializeConnectionTests()
    {
        RefreshConnectionTestSources();
        this.FindControl<Button>("ConnectionTestStart")!.Click += async (_, _) => await RunConnectionTestAsync();
        this.FindControl<Button>("ConnectionTestCancel")!.Click += (_, _) => _connectionTestStop?.Cancel();
        this.FindControl<Button>("ConnectionTestOpen")!.Click += (_, _) => NavigateTo(SelectedConnectionSource() switch
        { "Hardware" => 5, "HA" => 9, "Virtual" or "Serial" => 7, _ => 3 });
        this.FindControl<ComboBox>("ConnectionTestSource")!.SelectionChanged += (_, _) =>
        {
            if (!_connectionSourceRebinding && _connectionTestStop is null && SelectedConnectionSource() != _connectionTestSource)
            {
                _connectionTestResults = []; _connectionTestAt = null;
                RenderConnectionTestResults();
            }
        };
        UiLanguageChanged += () => { RefreshConnectionTestSources(); RenderConnectionTestResults(); };
        ActualThemeVariantChanged += (_, _) => RenderConnectionTestResults();
    }

    private string SelectedConnectionSource() => (this.FindControl<ComboBox>("ConnectionTestSource")!.SelectedItem as ConnectionSource)?.Id ?? "Virtual";

    private void RefreshConnectionTestSources()
    {
        var picker = this.FindControl<ComboBox>("ConnectionTestSource")!;
        var selected = SelectedConnectionSource();
        var sources = new List<ConnectionSource>
        {
            new("Virtual", UiText("Virtual LIS2")), new("Serial", UiText("Serial LIS2")),
            new("Hardware", UiText("Hardware")), new("HA", "Home Assistant")
        };
        if (OperatingSystem.IsWindows()) sources.AddRange([new("Winamp", "Winamp"),
            new("WindowsMedia", "Windows Media Player"), new("WmpLegacy", "Windows Media Player Legacy")]);
        if (OperatingSystem.IsLinux()) sources.Add(new("Media", UiText("Linux media")));
        _connectionSourceRebinding = true;
        try { picker.ItemsSource = sources; picker.SelectedItem = sources.First(source => source.Id == selected); }
        finally { _connectionSourceRebinding = false; }
    }

    internal IReadOnlyList<ConnectionTestStep> BuildConnectionTestPlan(string source) => source switch
    {
        "Virtual" => [ConnectionTest.Check("Virtual connection", _virtualDevice.IsConnected,
            "The virtual device is not connected", "Select Virtual LIS2 and apply the connection in Settings")],
        "Serial" => ConnectionTestPlans.Serial(_preferredPortName, _serialDisplay?.IsConnected == true, SerialPortDiscovery.GetAvailablePorts),
        "Hardware" => ConnectionTestPlans.Hardware(_hardwareSensors,
            _dataFreshness.Read("Hardware", DateTimeOffset.UtcNow, _qualityConnections.GetValueOrDefault("Hardware"), TimeSpan.FromSeconds(10)).State == "Current"),
        "HA" => new HomeAssistantConnectionProbe(this.FindControl<TextBox>("HaUrl")!.Text, this.FindControl<TextBox>("HaToken")!.Text).Steps(),
        "Winamp" => ConnectionTestPlans.Media(source, _mediaWinamp.Values, new Dictionary<string, object?>(), true, _ => false),
        "WmpLegacy" => ConnectionTestPlans.Media(source, _mediaLegacy.Values, new Dictionary<string, object?>(), true, _ => false),
        "WindowsMedia" => ConnectionTestPlans.Media(source, _windowsMedia.Values, new Dictionary<string, object?>(), _windowsMedia.IsAvailable, _ => false),
        "Media" => ConnectionTestPlans.Media(source, new Dictionary<string, object?>(_mprisValues)
        {
            ["SnapshotAgeSeconds"] = _mprisReceivedAt is { } received ? (DateTimeOffset.UtcNow - received).TotalSeconds : null
        }, _linuxAudio.Values, true, ConnectionTestPlans.ExecutableAvailable),
        _ => throw new ArgumentException("Unsupported connection source", nameof(source))
    };

    /// <summary>Testlauf und Shutdown teilen denselben Abbruchpfad; keine Geräteaktionen oder Einstellungen speichern.</summary>
    internal Task RunConnectionTestAsync(IReadOnlyList<ConnectionTestStep>? steps = null)
    {
        if (_connectionTestStop is not null) return _connectionTestTask;
        _connectionTestStop = new CancellationTokenSource();
        _connectionTestSource = SelectedConnectionSource();
        _connectionTestAt = DateTimeOffset.UtcNow;
        this.FindControl<ComboBox>("ConnectionTestSource")!.IsEnabled = false;
        this.FindControl<Button>("ConnectionTestStart")!.IsEnabled = false;
        this.FindControl<Button>("ConnectionTestCancel")!.IsEnabled = true;
        _connectionTestTask = ExecuteConnectionTestAsync(steps);
        return _connectionTestTask;
    }

    private async Task ExecuteConnectionTestAsync(IReadOnlyList<ConnectionTestStep>? steps)
    {
        try
        {
            steps ??= BuildConnectionTestPlan(_connectionTestSource!);
            _connectionTestResults = await ConnectionTest.RunAsync(steps, results =>
            {
                _connectionTestResults = results; RenderConnectionTestResults();
            }, _connectionTestStop!.Token);
        }
        catch (Exception exception)
        {
            _connectionTestResults = [new("Connection test", ConnectionTestState.Failed, ConnectionTest.SafeError(exception), "Open the source and check its settings")];
        }
        finally
        {
            _connectionTestStop!.Dispose(); _connectionTestStop = null;
            this.FindControl<ComboBox>("ConnectionTestSource")!.IsEnabled = true;
            this.FindControl<Button>("ConnectionTestStart")!.IsEnabled = true;
            this.FindControl<Button>("ConnectionTestCancel")!.IsEnabled = false;
            RenderConnectionTestResults();
        }
    }

    private void RenderConnectionTestResults()
    {
        var panel = this.FindControl<StackPanel>("ConnectionTestResults")!;
        panel.Children.Clear();
        foreach (var result in _connectionTestResults)
        {
            var row = new StackPanel { Spacing = 4 };
            var header = new Grid { ColumnDefinitions = new("*,Auto"), ColumnSpacing = 12 };
            header.Children.Add(new TextBlock { Text = UiText(result.Name), FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
            var state = new TextBlock { Text = UiText(result.State.ToString()), VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top };
            if (result.State == ConnectionTestState.Failed) state.Foreground = Brush.Parse(ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark ? "#FF9D9D" : "#B42318");
            Grid.SetColumn(state, 1); header.Children.Add(state); row.Children.Add(header);
            if (result.Detail.Length > 0) row.Children.Add(new TextBlock { Text = ConnectionTestText.TranslateDetail(result.Detail, CurrentUiLanguage()), TextWrapping = TextWrapping.Wrap });
            if (result.Remedy.Length > 0) row.Children.Add(new TextBlock { Text = UiText(result.Remedy), TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(row);
        }
        var summary = _connectionTestStop is not null ? "Connection test running" : _connectionTestResults.Count == 0 ? "Select a source and start the test" :
            _connectionTestResults.Any(result => result.State == ConnectionTestState.Cancelled) ? "Test cancelled" :
            _connectionTestResults.Any(result => result.State == ConnectionTestState.Failed) ? "Connection test failed" :
            _connectionTestResults.Any(result => result.State == ConnectionTestState.Skipped) ? "Connection test completed with skipped checks" : "Connection test passed";
        this.FindControl<TextBlock>("ConnectionTestStatus")!.Text = UiText(summary) + (_connectionTestAt is { } at ? " · " + at.ToLocalTime().ToString("G", CultureInfo.CurrentCulture) : "");
    }

    private async Task StopConnectionTestAsync()
    {
        _connectionTestStop?.Cancel();
        await _connectionTestTask;
    }
}
