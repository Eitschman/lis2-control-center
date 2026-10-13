using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LIS2.App.Avalonia;

public partial class MainWindow : Window
{
    private readonly VirtualDeviceSession _virtualDevice = new();
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();

        var navigation = this.FindControl<ListBox>("Navigation");
        if (navigation is not null)
            navigation.SelectionChanged += OnNavigationChanged;

        this.FindControl<Button>("VirtualConnect")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.ConnectAsync());
        this.FindControl<Button>("VirtualDemo")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.WriteDemoAsync());
        this.FindControl<Button>("VirtualDisconnect")!.Click += async (_, _) =>
            await RunVirtualActionAsync(() => _virtualDevice.DisconnectAsync());

        this.FindControl<Button>("OpenDisplayControls")!.Click += (_, _) => NavigateTo(1);
        this.FindControl<Button>("OpenFanControl")!.Click += (_, _) => NavigateTo(6);

        Closed += OnWindowClosed;
        UpdateSection();
        UpdateVirtualControls();
    }

    private async void OnWindowClosed(object? sender, EventArgs e)
    {
        await _virtualDevice.DisposeAsync();
    }

    private async Task RunVirtualActionAsync(Func<Task> action)
    {
        if (_busy)
            return;

        _busy = true;
        UpdateVirtualControls();
        try
        {
            await action();
            UpdatePreview();
            SetVirtualStatus(_virtualDevice.IsConnected
                ? $"Virtual LIS2: connected ({_virtualDevice.CommandCount} commands)"
                : "Virtual LIS2: disconnected");
        }
        catch (Exception ex)
        {
            SetVirtualStatus($"Virtual LIS2 error: {ex.Message}");
        }
        finally
        {
            _busy = false;
            UpdateVirtualControls();
        }
    }

    private void UpdatePreview()
    {
        var first = this.FindControl<TextBlock>("PreviewLine1");
        var second = this.FindControl<TextBlock>("PreviewLine2");
        if (first is null || second is null)
            return;

        if (_virtualDevice.IsConnected && _virtualDevice.CommandCount > 0)
        {
            first.Text = "LIS2 Control Center";
            second.Text = "Avalonia Windows/Linux";
        }
        else
        {
            first.Text = "                    ";
            second.Text = "                    ";
        }
    }

    private void SetVirtualStatus(string message)
    {
        var status = this.FindControl<TextBlock>("VirtualStatus");
        if (status is not null)
            status.Text = message;
    }

    private void UpdateVirtualControls()
    {
        var connected = _virtualDevice.IsConnected;
        this.FindControl<Button>("VirtualConnect")!.IsEnabled = !_busy && !connected;
        this.FindControl<Button>("VirtualDemo")!.IsEnabled = !_busy && connected;
        this.FindControl<Button>("VirtualDisconnect")!.IsEnabled = !_busy && connected;
        var badge = this.FindControl<TextBlock>("ConnectionText");
        if (badge is not null)
            badge.Text = connected ? "Connected (Virtual)" : "Disconnected";
        var virtualState = this.FindControl<TextBlock>("VirtualStateText");
        if (virtualState is not null)
            virtualState.Text = connected
                ? $"Virtual LIS2 connected. Commands: {_virtualDevice.CommandCount}."
                : "State available when Virtual transport is active.";
    }

    private void NavigateTo(int index)
    {
        var navigation = this.FindControl<ListBox>("Navigation");
        if (navigation is not null)
            navigation.SelectedIndex = index;
    }

    private void OnNavigationChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateSection();

    private void UpdateSection()
    {
        if (this.FindControl<ListBox>("Navigation") is not { SelectedItem: ListBoxItem item })
            return;

        var rawLabel = item.Content?.ToString() ?? "Dashboard";
        var section = rawLabel.Length > 5 ? rawLabel[5..].Trim() : rawLabel;
        var title = this.FindControl<TextBlock>("SectionTitle");
        var description = this.FindControl<TextBlock>("SectionDescription");

        if (title is not null)
            title.Text = section;

        if (description is not null)
            description.Text = section == "Dashboard"
                ? "Overview and quick access to the most important functions."
                : $"{section} is not connected to the LIS2 runtime yet.";
    }
}
