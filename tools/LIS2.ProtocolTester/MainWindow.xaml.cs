using System.Globalization;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using LIS2.Core;

namespace LIS2.ProtocolTester;

public partial class MainWindow : Window
{
    private ILis2Transport? _transport;
    private Lis2Device? _device;

    public MainWindow()
    {
        InitializeComponent();
        RefreshPorts();
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (_device is not null)
            await _device.DisposeAsync();
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private void RefreshPorts()
    {
        var selected = PortComboBox.SelectedItem as string;
        var ports = SerialPort.GetPortNames()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        PortComboBox.ItemsSource = ports;

        if (selected is not null && ports.Contains(selected))
            PortComboBox.SelectedItem = selected;
        else if (ports.Length > 0)
            PortComboBox.SelectedIndex = 0;

        Log($"INFO ports: {(ports.Length == 0 ? "<none>" : string.Join(", ", ports))}");
    }

    private void VirtualModeChanged(object sender, RoutedEventArgs e)
    {
        PortComboBox.IsEnabled = VirtualModeCheckBox.IsChecked != true;
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_device?.IsConnected == true)
            {
                await _device.DisposeAsync();
                _device = null;
                _transport = null;
                ConnectionStatus.Text = "Disconnected";
                ConnectButton.Content = "Connect";
                Log("INFO disconnected");
                return;
            }

            if (VirtualModeCheckBox.IsChecked == true)
            {
                var virtualTransport = new VirtualLis2Transport();
                virtualTransport.Written += VirtualTransport_Written;
                _transport = virtualTransport;
                Log("INFO using Virtual LIS2 transport");
            }
            else
            {
                if (PortComboBox.SelectedItem is not string portName)
                    throw new InvalidOperationException("Select a COM port first.");

                _transport = new SerialLis2Transport(portName);
                Log($"INFO using serial transport {portName} 19200 8N1; handshake=None; DTR=False; RTS=False");
            }

            _device = new Lis2Device(_transport);
            await _device.ConnectAsync();

            ConnectionStatus.Text = VirtualModeCheckBox.IsChecked == true
                ? "Connected: Virtual LIS2"
                : $"Connected: {PortComboBox.SelectedItem} @ 19200 8N1";
            ConnectButton.Content = "Disconnect";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void VirtualTransport_Written(object? sender, VirtualLis2WriteEventArgs e) =>
        Log($"VIRT {FormatHex(e.Data)}");

    private async void Clear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RequireDevice().ClearAsync();
            Log("CMD  Clear");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void SendLine1_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RequireDevice().WriteLineAsync(1, Line1TextBox.Text);
            Log($"CMD  Line1 \"{Line1TextBox.Text}\"");
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
            await RequireDevice().WriteLineAsync(2, Line2TextBox.Text);
            Log($"CMD  Line2 \"{Line2TextBox.Text}\"");
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
                _ => throw new InvalidOperationException("Unknown brightness selection.")
            };

            await RequireDevice().SetBrightnessAsync(brightness);
            Log($"CMD  Brightness {tag}%");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void SendFans_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var values = new[]
            {
                ParsePercent(Fan1TextBox.Text, "Fan 1"),
                ParsePercent(Fan2TextBox.Text, "Fan 2"),
                ParsePercent(Fan3TextBox.Text, "Fan 3"),
                ParsePercent(Fan4TextBox.Text, "Fan 4")
            };

            await RequireDevice().SetFansAsync(values[0], values[1], values[2], values[3]);
            Log($"CMD  Fans {string.Join("/", values)}%");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void ProgramCharacter_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(CharacterSlotTextBox.Text, out var slot) || slot is < 1 or > 8)
                throw new InvalidOperationException("Character slot must be between 1 and 8.");

            var rowTokens = CharacterRowsTextBox.Text
                .Split([' ', ',', ';', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            if (rowTokens.Length != 8)
                throw new InvalidOperationException("Enter exactly 8 row values.");

            var rows = rowTokens
                .Select(value => byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                .ToArray();

            if (rows.Any(value => value > 0x1F))
                throw new InvalidOperationException("Each custom-character row must be between 00 and 1F.");

            await RequireDevice().ProgramCharacterAsync(slot, rows);
            Log($"CMD  Program character slot {slot}: {FormatHex(rows)}");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private Lis2Device RequireDevice()
    {
        if (_device?.IsConnected != true)
            throw new InvalidOperationException("Connect to the LIS2 transport first.");

        return _device;
    }

    private static int ParsePercent(string text, string name)
    {
        if (!int.TryParse(text, out var value) || value is < 0 or > 100)
            throw new InvalidOperationException($"{name} must be between 0 and 100.");

        return value;
    }

    private static string FormatHex(IEnumerable<byte> bytes) =>
        string.Join(" ", bytes.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));

    private void ShowError(Exception ex)
    {
        Log($"ERR  {ex.Message}");
        MessageBox.Show(this, ex.Message, "LIS2 Protocol Tester", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void Log(string text)
    {
        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogTextBox.Clear();
}
