using System.Globalization;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;

namespace LIS2.ProtocolTester;

public partial class MainWindow : Window
{
    private readonly Lis2SerialConnection _connection = new();

    public MainWindow()
    {
        InitializeComponent();
        RefreshPorts();
        Closed += (_, _) => _connection.Dispose();
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

    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_connection.IsOpen)
            {
                _connection.Close();
                ConnectionStatus.Text = "Disconnected";
                ConnectButton.Content = "Connect";
                Log("INFO disconnected");
                return;
            }

            if (PortComboBox.SelectedItem is not string portName)
                throw new InvalidOperationException("Select a COM port first.");

            _connection.Open(portName);
            ConnectionStatus.Text = $"Connected: {portName} @ 19200 8N1";
            ConnectButton.Content = "Disconnect";
            Log($"INFO connected {portName} 19200 8N1; handshake=None; DTR=False; RTS=False");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) =>
        TrySend(Lis2Protocol.Clear, "Clear");

    private void SendLine1_Click(object sender, RoutedEventArgs e) =>
        TrySend(Lis2Protocol.WriteLine(1, 0, Line1TextBox.Text), $"Line1 \"{Line1TextBox.Text}\"");

    private void SendLine2_Click(object sender, RoutedEventArgs e) =>
        TrySend(Lis2Protocol.WriteLine(2, 0, Line2TextBox.Text), $"Line2 \"{Line2TextBox.Text}\"");

    private void Brightness_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag })
            return;

        var brightness = tag switch
        {
            "100" => Brightness.Percent100,
            "75" => Brightness.Percent75,
            "50" => Brightness.Percent50,
            "25" => Brightness.Percent25,
            _ => throw new InvalidOperationException("Unknown brightness selection.")
        };

        TrySend(Lis2Protocol.SetBrightness(brightness), $"Brightness {tag}%");
    }

    private void SendFans_Click(object sender, RoutedEventArgs e)
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

            var command = Lis2Protocol.SetFans(values[0], values[1], values[2], values[3]);
            TrySend(command, $"Fans {string.Join("/", values)}%");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void ProgramCharacter_Click(object sender, RoutedEventArgs e)
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
                .Select(value => int.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                .ToArray();

            if (rows.Any(value => value is < 0 or > 0x1F))
                throw new InvalidOperationException("Each custom-character row must be between 00 and 1F.");

            for (var row = 0; row < 8; row++)
                TrySend(Lis2Protocol.ProgramCharacterRow(slot, row, rows[row]), $"CG slot={slot} row={row} data={rows[row]:X2}");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private static int ParsePercent(string text, string name)
    {
        if (!int.TryParse(text, out var value) || value is < 0 or > 100)
            throw new InvalidOperationException($"{name} must be between 0 and 100.");
        return value;
    }

    private void TrySend(byte[] command, string semantic)
    {
        try
        {
            _connection.Send(command);
            Log($"TX   {Convert.ToHexString(command, " ")}    {semantic}");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

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
