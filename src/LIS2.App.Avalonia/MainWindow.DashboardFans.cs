using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private bool _dashboardFansBusy;

    private void InitializeDashboardFans()
    {
        for (var channel = 1; channel <= 4; channel++)
        {
            var number = channel;
            this.FindControl<Slider>($"DashboardFan{number}Slider")!.PropertyChanged += (_, args) =>
            {
                if (args.Property == Slider.ValueProperty)
                    this.FindControl<TextBlock>($"DashboardFan{number}Value")!.Text =
                        ((int)Math.Round(this.FindControl<Slider>($"DashboardFan{number}Slider")!.Value)) + " %";
            };
        }
        this.FindControl<CheckBox>("DashboardFanConsent")!.IsCheckedChanged += (_, _) => UpdateDashboardFanControls();
        this.FindControl<Button>("DashboardFansApply")!.Click += async (_, _) => await ApplyDashboardFansAsync();
    }

    /// <summary>Handausgabe bleibt bei laufender Regelung gesperrt; serielle Freigabe gilt je Aktion.</summary>
    private void UpdateDashboardFanControls()
    {
        var available = SelectedDisplay?.IsConnected == true && !_settingsTransportBusy && !_serialBusy &&
            !_busy && !_dashboardFansBusy && _serialDisplay?.AutomaticFansRunning != true &&
            (SerialSelected ? _serialSequence : _virtualSequence)?.IsBusy != true;
        for (var channel = 1; channel <= 4; channel++)
            this.FindControl<Slider>($"DashboardFan{channel}Slider")!.IsEnabled = available;
        this.FindControl<CheckBox>("DashboardFanConsent")!.IsVisible = SerialSelected;
        this.FindControl<Button>("DashboardFansApply")!.IsEnabled = available &&
            (!SerialSelected || this.FindControl<CheckBox>("DashboardFanConsent")!.IsChecked == true);
    }

    /// <summary>WPF-Kanalgrenzen gelten auch für die explizit freizugebende Dashboard-Ausgabe.</summary>
    private async Task ApplyDashboardFansAsync()
    {
        if (_dashboardFansBusy || _settingsTransportBusy || _serialBusy || _busy || SelectedDisplay?.IsConnected != true ||
            _serialDisplay?.AutomaticFansRunning == true || (SerialSelected ? _serialSequence : _virtualSequence)?.IsBusy == true) return;
        var authorized = this.FindControl<CheckBox>("DashboardFanConsent")!.IsChecked == true;
        if (SerialSelected && !authorized) return;
        _dashboardFansBusy = true;
        UpdateVirtualControls();
        this.FindControl<CheckBox>("DashboardFanConsent")!.IsChecked = false;
        UpdateDashboardFanControls();
        try
        {
            var requested = Enumerable.Range(1, 4).Select(channel =>
                (int)Math.Round(this.FindControl<Slider>($"DashboardFan{channel}Slider")!.Value)).ToArray();
            var outputs = _fanSettings.CalculateManualOutputs(requested);
            await RunSelectedDisplayActionAsync(async _ =>
            {
                if (SerialSelected)
                    await _serialDisplay!.ApplyManualFansAsync(outputs, authorized);
                else await _virtualDevice.SetFansAsync(outputs);
                for (var channel = 0; channel < 4; channel++)
                {
                    _fanSettings.Channels[channel].FixedPercent = requested[channel];
                    this.FindControl<Slider>($"DashboardFan{channel + 1}Slider")!.Value = outputs[channel];
                }
                await SavePortableWorkspaceAsync(true);
                SetVirtualStatus("Lüfterausgabe: " + string.Join(" / ", outputs.Select(value => value + " %")));
            });
        }
        catch (Exception ex) { SetVirtualStatus("Lüfterausgabe fehlgeschlagen: " + ex.Message); }
        finally { _dashboardFansBusy = false; UpdateHardwareFanControls(); UpdateVirtualControls(); }
    }
}
