using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly DataFreshness _dataFreshness = new();
    private readonly HashSet<string> _discoveredVariableKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _validationSignatures = new();
    private readonly Dictionary<string, bool> _qualityConnections = new();
    private readonly DispatcherTimer _freshnessTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTimeOffset? _mprisReceivedAt;

    /// <summary>Ein anderer Player darf keinen Empfangszeitpunkt seines Vorgängers übernehmen.</summary>
    internal void ResetLinuxMediaQuality()
    {
        _mprisReceivedAt = null;
        _dataFreshness.Reset("Media");
        _qualityConnections["Media"] = false;
    }

    /// <summary>Alter unabhängig von Ansichtswechseln aktualisieren; keine zusätzlichen Quellenabfragen starten.</summary>
    private void InitializeSourceQuality()
    {
        _freshnessTimer.Tick += (_, _) => RefreshDataFreshness();
        _freshnessTimer.Start();
        Closed += (_, _) => _freshnessTimer.Stop();
        UiLanguageChanged += () => { RefreshTemplateValidation(); RefreshDataFreshness(); };
        RefreshDataFreshness();
    }

    private string QualityText(string source) => SourceQualityText.Get(source, CurrentUiLanguage());

    internal void RefreshTemplateValidation()
    {
        if (_selectedSection != 2 || _loadingPage || SelectedPageIndex is null) return;
        var values = PagePreviewValues();
        _discoveredVariableKeys.UnionWith(values.Keys.Where(key => !key.StartsWith("Glyph.", StringComparison.OrdinalIgnoreCase)));
        var known = _discoveredVariableKeys.Concat(HelpVariableCatalog.Variables.Select(entry => entry.Key))
            .Concat(_hardwarePreferences.Select(preference => preference.Key)).ToArray();
        foreach (var name in new[] { "PageLine1", "PageLine2" })
        {
            var input = this.FindControl<TextBox>(name)!;
            var issues = TemplateValidation.Check(input.Text ?? "", values, known);
            var dark = ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark;
            var signature = CurrentUiLanguage() + "|" + dark + "|" + string.Join("|", issues);
            if (_validationSignatures.GetValueOrDefault(name) == signature) continue;
            _validationSignatures[name] = signature;
            var panel = this.FindControl<StackPanel>(name + "Issues")!;
            panel.Children.Clear();
            panel.IsVisible = issues.Count > 0;
            if (issues.Any(issue => issue.IsError)) input.BorderBrush = Brushes.IndianRed;
            else input.ClearValue(TextBox.BorderBrushProperty);
            foreach (var issue in issues)
            {
                var button = new Button
                {
                    Padding = new(3), HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = Brushes.Transparent,
                    Content = new TextBlock
                    {
                        Text = $"{QualityText(issue.Code)}: {issue.Detail}", TextWrapping = TextWrapping.Wrap,
                        Foreground = Brush.Parse(issue.IsError ? dark ? "#FF9D9D" : "#B42318" : dark ? "#F3C76A" : "#865600"), FontSize = 12
                    }
                };
                button.Click += (_, _) =>
                {
                    input.Focus();
                    input.SelectionStart = Math.Min(issue.Start, input.Text?.Length ?? 0);
                    input.SelectionEnd = Math.Min(issue.Start + issue.Length, input.Text?.Length ?? 0);
                };
                panel.Children.Add(button);
            }
        }
    }

    internal void ObserveHardwareQuality(IReadOnlyDictionary<string, object?> values, DateTimeOffset now)
    {
        var valid = values.Any(pair => DataFreshness.IsValidSample(pair.Value));
        _dataFreshness.Observe("Hardware", now, valid);
        _qualityConnections["Hardware"] = valid;
        foreach (var pair in values)
        {
            var available = DataFreshness.IsValidSample(pair.Value);
            _dataFreshness.Observe(pair.Key, now, available);
            _qualityConnections[pair.Key] = available;
        }
        foreach (var key in _qualityConnections.Keys.Where(key => key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase)).ToArray())
            if (!values.ContainsKey(key)) _qualityConnections[key] = false;
        RefreshDataFreshness();
    }

    internal void ObserveMediaQuality(string id, IReadOnlyDictionary<string, object?> values, DateTimeOffset now)
    {
        var connected = values.GetValueOrDefault("Connected") is true;
        _qualityConnections[id] = connected;
        DateTimeOffset? received = id == "Media" ? _mprisReceivedAt :
            values.GetValueOrDefault("SnapshotAgeSeconds") is double age && double.IsFinite(age) && age >= 0 && age <= TimeSpan.MaxValue.TotalSeconds / 2
                ? SafeReceivedAt(now, age) : null;
        _dataFreshness.Observe(id, received, connected);
    }

    private static DateTimeOffset? SafeReceivedAt(DateTimeOffset now, double age) =>
        age <= (now - DateTimeOffset.MinValue).TotalSeconds ? now.AddSeconds(-age) : null;

    internal void RefreshDataFreshness()
    {
        var now = DateTimeOffset.UtcNow;
        var ha = _pageSources.HomeAssistant;
        if (ha.LastReceivedAt is null) _dataFreshness.Reset("HA");
        else _dataFreshness.Observe("HA", ha.LastReceivedAt, true);
        ShowFreshness("HardwareFreshness", "Hardware", now, TimeSpan.FromSeconds(10));
        var selected = _selectedHardwareSensorKey;
        this.FindControl<TextBlock>("HardwareSelectedFreshness")!.IsVisible = selected is not null;
        if (selected is not null) ShowFreshness("HardwareSelectedFreshness", selected, now, TimeSpan.FromSeconds(10));
        // Ereignisquellen dürfen minutenlang unverändert sein; ihr Datenalter ist kein Timeout.
        _qualityConnections["HA"] = ha.IsConnected;
        ShowFreshness("HaFreshness", "HA", now, null, true);
        ShowFreshness("WindowsMediaFreshness", "WindowsMedia", now, null);
        ShowFreshness("WinampMediaFreshness", "Winamp", now, TimeSpan.FromSeconds(5));
        ShowFreshness("WmpLegacyMediaFreshness", "WmpLegacy", now, TimeSpan.FromSeconds(5));
        ShowFreshness("LinuxMediaFreshness", "Media", now, TimeSpan.FromSeconds(5));
    }

    private void ShowFreshness(string control, string key, DateTimeOffset now, TimeSpan? staleAfter, bool reception = false)
    {
        var info = _dataFreshness.Read(key, now, _qualityConnections.GetValueOrDefault(key), staleAfter);
        var text = QualityText(key.StartsWith("Hardware.", StringComparison.OrdinalIgnoreCase) && info.State == "Disconnected"
            ? "Currently unavailable" : info.State);
        if (info.LastValidAt is { } timestamp)
            text += $" · {QualityText(reception ? "Last data received" : "Last valid data")}: {timestamp.ToLocalTime().ToString("G", CultureInfo.CurrentCulture)} · {info.AgeSeconds:0} s";
        this.FindControl<TextBlock>(control)!.Text = text;
    }
}
