using System.Globalization;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LIS2.Core;
using LIS2.Display;
using LIS2.Sources;

namespace LIS2.App;

public partial class MainWindow : Window
{
    private readonly SettingsStore _settingsStore = new();
    private readonly DataSourceRegistry _sources = new();
    private readonly PageScheduler _pageScheduler = new();
    private readonly EventQueue _eventQueue = new();
    private readonly DisplayRuntime _displayRuntime;
    private readonly DispatcherTimer _pageTimer;

    private AppSettings _settings = new();
    private ILis2Transport? _transport;
    private Lis2Device? _device;
    private DisplayFrame _frame = DisplayFrame.Create(string.Empty, string.Empty);

    public MainWindow()
    {
        InitializeComponent();

        _displayRuntime = new DisplayRuntime(
            new TemplateRenderer(),
            _pageScheduler,
            _eventQueue);

        _pageTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _pageTimer.Tick += PageTimer_Tick;

        _sources.Add(new ClockDataSource());

        _pageScheduler.ReplacePages(new[]
        {
            new DisplayPage(
                "clock",
                "Clock",
                "{Clock.Time}",
                "{Clock.Date}",
                TimeSpan.FromSeconds(5)),
            new DisplayPage(
                "status",
                "Status",
                "LIS2 Control Center",
                "Virtual/Serial ready",
                TimeSpan.FromSeconds(5))
        });

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = await _settingsStore.LoadAsync();
            RefreshPorts();
            ApplySettingsToUi();
            await ReconnectAsync();

            await _sources.StartAllAsync();
            await RenderRuntimePageAsync();
            _pageTimer.Start();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        _pageTimer.Stop();

        await _sources.StopAllAsync();
        await _sources.DisposeAsync();

        if (_device is not null)
            await _device.DisposeAsync();
    }

    private async void PageTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            await RenderRuntimePageAsync();
        }
        catch (Exception ex)
        {
            Log($"ERR  page runtime: {ex.Message}");
        }
    }

    private async Task RenderRuntimePageAsync()
    {
        var nextFrame = _displayRuntime.RenderNext(
            _sources.Snapshot(),
            DateTimeOffset.Now);

        if (nextFrame is null)
            return;

        _frame = nextFrame;
        await WriteFrameAsync(nextFrame);
        RefreshPreview();
    }

    private async Task WriteFrameAsync(DisplayFrame frame)
    {
        var device = RequireDevice();
        await device.WriteLineAsync(1, frame.Line1);
        await device.WriteLineAsync(2, frame.Line2);
    }

    private void ApplySettingsToUi()
    {
        TransportModeComboBox.SelectedIndex =
            string.Equals(_settings.TransportMode, "Serial", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        if (_settings.PortName is not null && PortComboBox.Items.Contains(_settings.PortName))
            PortComboBox.SelectedItem = _settings.PortName;

        UpdateTransportUi();
    }

    private void RefreshPorts()
    {
        var selected = PortComboBox.SelectedItem as string ?? _settings.PortName;
        var ports = SerialPort.GetPortNames()
            .OrderBy(port => port, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        PortComboBox.ItemsSource = ports;

        if (selected is not null && ports.Contains(selected))
            PortComboBox.SelectedItem = selected;
        else if (ports.Length > 0)
            PortComboBox.SelectedIndex = 0;
    }

    private async Task ReconnectAsync()
    {
        if (_device is not null)
        {
            await _device.DisposeAsync();
            _device = null;
            _transport = null;
        }

        if (string.Equals(_settings.TransportMode, "Serial", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_settings.PortName))
                throw new InvalidOperationException("Select a COM port before using the serial transport.");

            _transport = new SerialLis2Transport(_settings.PortName);
        }
        else
        {
            var virtualTransport = new VirtualLis2Transport();
            virtualTransport.Written += Transport_Written;
            _transport = virtualTransport;
        }

        _device = new Lis2Device(_transport);
        await _device.ConnectAsync();

        ConnectionText.Text = _settings.TransportMode == "Serial"
            ? $"Connected: {_settings.PortName}"
            : "Virtual LIS2 connected";

        TransportSummaryText.Text = _settings.TransportMode == "Serial"
            ? $"Serial: {_settings.PortName}"
            : "Virtual LIS2 transport";

        Log($"INFO connected using {_settings.TransportMode} transport");
    }

    private void TransportModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        UpdateTransportUi();
    }

    private void UpdateTransportUi()
    {
        var serial = TransportModeComboBox.SelectedIndex == 1;
        PortComboBox.IsEnabled = serial;
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private async void ApplyDeviceSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.TransportMode = TransportModeComboBox.SelectedIndex == 1 ? "Serial" : "Virtual";
            _settings.PortName = PortComboBox.SelectedItem as string;
            await _settingsStore.SaveAsync(_settings);
            await ReconnectAsync();
            await WriteFrameAsync(_frame);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        PortComboBox.Focus();

    private async void SendLine1_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _frame = DisplayFrame.Create(Line1TextBox.Text, _frame.Line2);
            await RequireDevice().WriteLineAsync(1, _frame.Line1);
            RefreshPreview();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void SendLine2_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _frame = DisplayFrame.Create(_frame.Line1, Line2TextBox.Text);
            await RequireDevice().WriteLineAsync(2, _frame.Line2);
            RefreshPreview();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void RenderBoth_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _frame = DisplayFrame.Create(Line1TextBox.Text, Line2TextBox.Text);
            await WriteFrameAsync(_frame);
            RefreshPreview();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void Clear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RequireDevice().ClearAsync();
            _frame = DisplayFrame.Create(string.Empty, string.Empty);
            RefreshPreview();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void Brightness_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: string tag })
                return;

            var brightness = tag switch
            {
                "100" => Lis2Brightness.Percent100,
                "75" => Lis2Brightness.Percent75,
                "50" => Lis2Brightness.Percent50,
                "25" => Lis2Brightness.Percent25,
                _ => throw new InvalidOperationException("Unknown brightness.")
            };

            _settings.BrightnessPercent = int.Parse(tag, CultureInfo.InvariantCulture);
            await _settingsStore.SaveAsync(_settings);
            await RequireDevice().SetBrightnessAsync(brightness);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void Fans_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var fan1 = ParsePercent(Fan1TextBox.Text, "Fan 1");
            var fan2 = ParsePercent(Fan2TextBox.Text, "Fan 2");
            var fan3 = ParsePercent(Fan3TextBox.Text, "Fan 3");
            var fan4 = ParsePercent(Fan4TextBox.Text, "Fan 4");

            await RequireDevice().SetFansAsync(fan1, fan2, fan3, fan4);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void Transport_Written(object? sender, VirtualLis2WriteEventArgs e)
    {
        var hex = string.Join(" ", e.Data.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        Dispatcher.Invoke(() => Log($"TX   {hex}"));
    }

    private Lis2Device RequireDevice() =>
        _device?.IsConnected == true
            ? _device
            : throw new InvalidOperationException("LIS2 device is not connected.");

    private void RefreshPreview()
    {
        PreviewLine1.Text = _frame.Line1;
        PreviewLine2.Text = _frame.Line2;
    }

    private static int ParsePercent(string value, string label)
    {
        if (!int.TryParse(value, out var result) || result is < 0 or > 100)
            throw new InvalidOperationException($"{label} must be between 0 and 100.");

        return result;
    }

    private void ShowError(Exception ex)
    {
        Log($"ERR  {ex.Message}");
        MessageBox.Show(this, ex.Message, "LIS2 Control Center", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void Log(string text)
    {
        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }
}
