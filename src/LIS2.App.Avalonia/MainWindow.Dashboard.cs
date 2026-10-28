using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LIS2.Display;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private DashboardSettings _dashboardSettings = DashboardSettings.Normalize(null);
    private readonly DispatcherTimer _dashboardTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private bool _dashboardRebinding;
    private sealed record DashboardPageChoice(string Id, string Name)
    {
        public override string ToString() => Name;
    }

    private void InitializeDashboard()
    {
        this.FindControl<Button>("DashboardPrevious")!.Click += async (_, _) => await ControlDashboardPagesAsync(direction: -1);
        this.FindControl<Button>("DashboardNext")!.Click += async (_, _) => await ControlDashboardPagesAsync(direction: 1);
        this.FindControl<Button>("DashboardPause")!.Click += async (_, _) =>
            await ControlDashboardPagesAsync(pause: !(ActiveDashboardSequence?.RotationPaused == true));
        this.FindControl<ComboBox>("DashboardPagePicker")!.SelectionChanged += async (_, _) =>
        {
            if (!_dashboardRebinding && this.FindControl<ComboBox>("DashboardPagePicker")!.SelectedItem is DashboardPageChoice page)
                await ControlDashboardPagesAsync(page.Id);
        };
        this.FindControl<Button>("DashboardValueSettings")!.Click += async (_, _) => await ShowDashboardSettingsAsync(false);
        this.FindControl<Button>("DashboardFanSettings")!.Click += async (_, _) => await ShowDashboardSettingsAsync(true);
        _dashboardTimer.Tick += (_, _) =>
        {
            if (_selectedSection == 0) { RefreshDashboardPlayback(); RefreshDashboardValues(); }
        };
        _dashboardTimer.Start();
        UpdateDashboardLayout();
        RefreshDashboardValues();
    }

    private PageSequenceController? ActiveDashboardSequence => SerialSelected ? _serialSequence : _virtualSequence;

    /// <summary>Manuelle Seitenwahl verwendet den aktiven Transport und dieselben Laufzeitwerte wie die Rotation.</summary>
    private async Task ControlDashboardPagesAsync(string? pageId = null, int direction = 0, bool? pause = null)
    {
        var sequence = ActiveDashboardSequence;
        if (sequence?.IsRunning != true || (SerialSelected ? _eventStartedSerial : _eventStartedVirtual) ||
            sequence.IsBusy || _busy || _serialBusy || _settingsTransportBusy) return;
        await sequence.ControlAsync(pageId, direction, pause);
        if (SerialSelected) await TickSerialPlaybackAsync();
        else await TickPagePlaybackAsync();
        RefreshDashboardPlayback();
    }

    private void RefreshDashboardPlayback()
    {
        var picker = this.FindControl<ComboBox>("DashboardPagePicker");
        if (picker is null) return;
        var sequence = ActiveDashboardSequence;
        var values = PagePreviewValues();
        var visibility = new VisibilityEvaluator();
        var choices = _pages.Pages.Where(page => page.Enabled && visibility.IsVisible(page.VisibilityExpression, values))
            .Select(page => new DashboardPageChoice(page.Id, page.Name)).ToArray();
        _dashboardRebinding = true;
        try
        {
            if (picker.ItemsSource is not DashboardPageChoice[] previous || !previous.SequenceEqual(choices)) picker.ItemsSource = choices;
            picker.SelectedItem = (picker.ItemsSource as DashboardPageChoice[])?.FirstOrDefault(page => page.Id == sequence?.CurrentPageId);
        }
        finally { _dashboardRebinding = false; }
        var enabled = sequence?.IsRunning == true && !(SerialSelected ? _eventStartedSerial : _eventStartedVirtual) &&
            !sequence.IsBusy && !_busy && !_serialBusy && !_settingsTransportBusy;
        picker.IsEnabled = enabled && choices.Length > 0;
        foreach (var name in new[] { "DashboardPrevious", "DashboardNext" })
            this.FindControl<Button>(name)!.IsEnabled = enabled && choices.Length > 1;
        var pause = this.FindControl<Button>("DashboardPause")!;
        pause.IsEnabled = enabled;
        this.FindControl<global::Avalonia.Controls.Shapes.Path>("DashboardPauseIcon")!.Data =
            Geometry.Parse(sequence?.RotationPaused == true ? "M4,2 L13,8 L4,14 Z" : "M5,2 L5,14 M11,2 L11,14");
        var action = UiText(sequence?.RotationPaused == true ? "Resume rotation" : "Pause rotation");
        ToolTip.SetTip(pause, action);
        AutomationProperties.SetName(pause, action);
        foreach (var (name, text) in new[] { ("DashboardPrevious", "Previous page"), ("DashboardNext", "Next page"),
            ("DashboardValueSettings", "Configure live values"), ("DashboardFanSettings", "Configure speed sensors") })
            AutomationProperties.SetName(this.FindControl<Button>(name)!, UiText(text));
    }

    /// <summary>Die kompakte VFD-Karte passt auch bei Mindestbreite; schmale Fenster zeigen Livewerte in zwei Spalten.</summary>
    private void UpdateDashboardLayout()
    {
        var layout = this.FindControl<Grid>("DashboardVfdLayout");
        if (layout is null) return;
        var stacked = Bounds.Width < 1100;
        var values = this.FindControl<Grid>("DashboardValuesLayout")!;
        values.ColumnDefinitions = new ColumnDefinitions(stacked ? "*,*" : "*,*,*,*");
        values.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto,Auto,Auto" : "Auto,Auto");
        var columns = stacked ? 2 : 4;
        for (var index = 0; index < DashboardSettings.ValueCount; index++)
        {
            var slot = this.FindControl<StackPanel>($"DashboardValueSlot{index + 1}")!;
            Grid.SetColumn(slot, index % columns);
            Grid.SetRow(slot, index / columns);
        }
    }

    private void RefreshDashboardValues()
    {
        var values = PagePreviewValues();
        var renderer = new TemplateRenderer();
        for (var index = 0; index < DashboardSettings.ValueCount; index++)
        {
            var setting = _dashboardSettings.Values[index];
            var label = this.FindControl<TextBlock>($"DashboardValueLabel{index + 1}")!;
            label.Text = setting.Label.Length == 0 ? UiText("Value") + " " + (index + 1) : setting.Label;
            var output = renderer.Render(setting.Template, values);
            var text = this.FindControl<TextBlock>($"DashboardValueText{index + 1}")!;
            text.Text = output.Length == 0 ? UiText("Unavailable") : output;
            ToolTip.SetTip(text, setting.Template);
        }
        for (var index = 0; index < 4; index++)
        {
            var channel = _fanSettings.Channels[index];
            if (SerialSelected && _serialDisplay?.AutomaticFansRunning == true && _serialDisplay.FanOutputs is { Count: 4 } outputs)
                this.FindControl<Slider>($"DashboardFan{index + 1}Slider")!.Value = outputs[index];
            this.FindControl<TextBlock>($"DashboardFan{index + 1}Mode")!.Text = UiText(channel.Mode);
            var speedKey = _dashboardSettings.FanSpeedKeys[index];
            var rpm = TryDashboardNumber(values.GetValueOrDefault(speedKey), out var speed) && speed >= 0
                ? speed.ToString("N0", CultureInfo.CurrentCulture) + " RPM" : UiText("Unavailable");
            this.FindControl<TextBlock>($"DashboardFan{index + 1}Rpm")!.Text = rpm;
            var sensor = this.FindControl<TextBlock>($"DashboardFan{index + 1}Sensor")!;
            sensor.Text = "";
            if (!string.IsNullOrWhiteSpace(channel.SensorKey))
            {
                var entry = HardwareSensorCatalog.Build(_hardwareSensors, _hardwarePreferences, trackedKeys: [channel.SensorKey])
                    .FirstOrDefault(item => item.Key.Equals(channel.SensorKey, StringComparison.OrdinalIgnoreCase));
                var name = entry?.DisplayName ?? channel.SensorKey;
                var number = TryDashboardNumber(values.GetValueOrDefault(channel.SensorKey), out var temperature)
                    ? temperature.ToString("0.#", CultureInfo.CurrentCulture) + " " + values.GetValueOrDefault(channel.SensorKey + ".Unit")
                    : UiText("Unavailable");
                sensor.Text = name + " " + number;
                sensor.MaxWidth = Bounds.Width < 1100 ? 240 : 380;
                sensor.TextTrimming = TextTrimming.CharacterEllipsis;
                ToolTip.SetTip(sensor, sensor.Text);
            }
        }
    }

    private static bool TryDashboardNumber(object? value, out double number) =>
        double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float,
            CultureInfo.InvariantCulture, out number) && double.IsFinite(number);

    /// <summary>Bearbeitung bleibt bis zum erfolgreichen Speichern ein Entwurf; Abbrechen verändert keine Einstellung.</summary>
    private async Task ShowDashboardSettingsAsync(bool fanSpeeds)
    {
        var draft = DashboardSettings.Normalize(_dashboardSettings);
        var dialog = new Window
        {
            Title = UiText(fanSpeeds ? "Configure speed sensors" : "Configure live values"), Icon = Icon,
            Width = 740, Height = Math.Min(fanSpeeds ? 440 : 720, Math.Max(360, Bounds.Height - 60)), MinWidth = 560, MinHeight = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var rows = new StackPanel { Spacing = 18 };
        var labels = new List<TextBox>();
        var inputs = new List<TextBox>();
        var count = fanSpeeds ? 4 : DashboardSettings.ValueCount;
        for (var index = 0; index < count; index++)
        {
            var number = index;
            var section = new StackPanel { Spacing = 6 };
            section.Children.Add(new TextBlock { Text = UiText(fanSpeeds ? "Fan " + (index + 1) : "Value") + (fanSpeeds ? "" : " " + (index + 1)), FontWeight = FontWeight.SemiBold });
            if (!fanSpeeds)
            {
                section.Children.Add(new TextBlock { Text = UiText("Name") });
                var label = new TextBox { Name = "DashboardLabel" + (index + 1), Text = draft.Values[index].Label };
                labels.Add(label); section.Children.Add(label);
            }
            section.Children.Add(new TextBlock { Text = UiText(fanSpeeds ? "Speed sensor key" : "Template") });
            var input = new TextBox { Name = "DashboardTemplate" + (index + 1), Text = fanSpeeds ? draft.FanSpeedKeys[index] : draft.Values[index].Template };
            if (!fanSpeeds) ToolTip.SetTip(input, UiText("Formatting: {Key|number:2}\n50.1234567 → 50,12 (German regional settings)\nfallback, fallbackvar, percent, bytes, prefix, suffix, upper, lower\nMore examples: Pages help"));
            inputs.Add(input);
            var browse = new Button { Name = "DashboardBrowse" + (index + 1), Content = UiText("Browse variables…") };
            browse.Click += async (_, _) => await ShowPageVariableBrowserAsync(selectedAction: key =>
            {
                if (fanSpeeds) inputs[number].Text = key;
                else
                {
                    var field = inputs[number];
                    var position = Math.Clamp(field.CaretIndex, 0, (field.Text ?? "").Length);
                    field.Text = (field.Text ?? "").Insert(position, "{" + key + "}");
                }
            });
            var line = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8 };
            Grid.SetColumn(browse, 1); line.Children.Add(input); line.Children.Add(browse);
            section.Children.Add(line); rows.Children.Add(section);
        }
        var error = new TextBlock { Name = "DashboardSettingsError", TextWrapping = TextWrapping.Wrap };
        var save = new Button { Name = "DashboardSettingsSave", Content = UiText("Save") };
        var cancel = new Button { Name = "DashboardSettingsCancel", Content = UiText("Cancel") };
        cancel.Click += (_, _) => dialog.Close();
        save.Click += async (_, _) =>
        {
            for (var index = 0; index < count; index++)
            {
                if (fanSpeeds) draft.FanSpeedKeys[index] = inputs[index].Text?.Trim() ?? "";
                else draft.Values[index] = new DashboardValueSetting { Label = labels[index].Text?.Trim() ?? "", Template = inputs[index].Text ?? "" };
            }
            var original = _dashboardSettings;
            _dashboardSettings = DashboardSettings.Normalize(draft);
            try
            {
                save.IsEnabled = false;
                cancel.IsEnabled = false;
                await SavePortableWorkspaceAsync(true);
                RefreshDashboardValues(); dialog.Close();
            }
            catch (Exception exception)
            {
                _dashboardSettings = original;
                error.Text = ((UiMessage)$"Workspace save failed: {(UiMessage)exception.Message}").Render(CurrentUiLanguage());
            }
            finally { save.IsEnabled = true; cancel.IsEnabled = true; }
        };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(save); actions.Children.Add(cancel);
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto,Auto"), RowSpacing = 12, Margin = new Thickness(18) };
        layout.Children.Add(new ScrollViewer { Content = rows });
        Grid.SetRow(error, 1); layout.Children.Add(error);
        Grid.SetRow(actions, 2); layout.Children.Add(actions);
        dialog.Content = layout;
        await dialog.ShowDialog(this);
    }
}
