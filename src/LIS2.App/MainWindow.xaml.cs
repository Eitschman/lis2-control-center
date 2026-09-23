using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LIS2.Core;
using LIS2.Display;

namespace LIS2.App;

public partial class MainWindow : Window
{
    private readonly VirtualLis2Transport _transport = new();
    private readonly Lis2Device _device;
    private DisplayFrame _frame = DisplayFrame.Create(string.Empty, string.Empty);

    public MainWindow()
    {
        InitializeComponent();

        _transport.Written += Transport_Written;
        _device = new Lis2Device(_transport);

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _device.ConnectAsync();
            ConnectionText.Text = "Virtual LIS2 connected";
            Log("INFO Virtual LIS2 connected");
            await RenderBothAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        await _device.DisposeAsync();
    }

    private async void SendLine1_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _frame = DisplayFrame.Create(Line1TextBox.Text, _frame.Line2);
            await _device.WriteLineAsync(1, _frame.Line1);
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
            await _device.WriteLineAsync(2, _frame.Line2);
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
            await RenderBothAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task RenderBothAsync()
    {
        _frame = DisplayFrame.Create(Line1TextBox.Text, Line2TextBox.Text);
        await _device.WriteLineAsync(1, _frame.Line1);
        await _device.WriteLineAsync(2, _frame.Line2);
        RefreshPreview();
    }

    private async void Clear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _device.ClearAsync();
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

            await _device.SetBrightnessAsync(brightness);
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

            await _device.SetFansAsync(fan1, fan2, fan3, fan4);
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
