using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Display;
using System.Globalization;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private sealed record EventQueueRow(string PriorityName, string DisplayName, string Remaining);

    private EventQueue _eventQueue => _runtime.Events;
    private readonly DispatcherTimer _eventTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private bool _eventStartedVirtual;
    private bool _eventStartedSerial;
    private bool _updatingEventOverlay;

    private void InitializeEvents()
    {
        this.FindControl<Button>("EventShow")!.Click += async (_, _) => await AddDisplayEventAsync(false);
        this.FindControl<Button>("EventTestWarning")!.Click += async (_, _) => await AddDisplayEventAsync(true);
        this.FindControl<Button>("EventClear")!.Click += async (_, _) =>
        {
            _eventQueue.Clear();
            await UpdateEventOverlayAsync();
        };
        this.FindControl<Button>("EventCancel")!.Click += async (_, _) =>
        {
            var selected = this.FindControl<ListBox>("EventList")!.SelectedIndex;
            var snapshot = _eventQueue.Snapshot(DateTimeOffset.UtcNow);
            if (selected >= 0 && selected < snapshot.Count)
                _eventQueue.Remove(snapshot[selected].Id);
            await UpdateEventOverlayAsync();
        };
        _eventTimer.Tick += async (_, _) =>
        {
            _eventQueue.Snapshot(DateTimeOffset.UtcNow);
            await UpdateEventOverlayAsync();
        };
        RefreshEventList();
    }

    private async Task AddDisplayEventAsync(bool warning)
    {
        var status = this.FindControl<TextBlock>("EventStatus")!;
        if (!int.TryParse(this.FindControl<TextBox>("EventDuration")!.Text, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var seconds) || seconds is < 1 or > 3600)
        {
            status.Text = "Duration must be between 1 and 3600 seconds.";
            return;
        }
        var priority = warning ? 100 : this.FindControl<ComboBox>("EventPriority")!.SelectedIndex switch
        {
            0 => 10, 1 => 50, 2 => 100, 3 => 200, _ => 50
        };
        var line1 = warning ? "WARNING" : this.FindControl<TextBox>("EventLine1")!.Text;
        var line2 = warning ? "Display event test" : this.FindControl<TextBox>("EventLine2")!.Text;
        _eventQueue.Add(new DisplayEvent(
            Guid.NewGuid().ToString("N"),
            DisplayFrame.Create(line1, line2),
            priority,
            DateTimeOffset.UtcNow.AddSeconds(seconds)));
        await UpdateEventOverlayAsync();
    }

    private void RefreshEventList()
    {
        var snapshot = _eventQueue.Snapshot(DateTimeOffset.UtcNow);
        var list = this.FindControl<ListBox>("EventList")!;
        var selected = list.SelectedIndex;
        list.ItemsSource = snapshot.Select(item => new EventQueueRow(
            item.Priority switch
            {
                >= 200 => "Critical",
                >= 100 => "Warning",
                >= 50 => "Notice",
                _ => "Info"
            },
            item.Frame.Line1.Trim() + " | " + item.Frame.Line2.Trim(),
            Math.Max(0, (int)Math.Ceiling((item.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds)) + "s"
        )).ToArray();
        list.SelectedIndex = selected >= 0 && selected < snapshot.Count ? selected : -1;
        this.FindControl<TextBlock>("EventCount")!.Text = $"{snapshot.Count} events";
    }

    private async Task UpdateEventOverlayAsync()
    {
        // The runtime owns the single priority queue consumed by BOTH transports.
        // Never write event frames directly: that would bypass the sequence's
        // frame tracking and race its scheduled display writes.
        if (_updatingEventOverlay) return;
        _updatingEventOverlay = true;
        try
        {
            var top = _eventQueue.Peek(DateTimeOffset.Now);
            RefreshEventList();
            if (top is null)
            {
                _eventTimer.Stop();

                // An event-only sequence has no regular pages. Tick once after
                // expiry to clear its last event frame before stopping it.
                if (_eventStartedVirtual)
                {
                    if (_virtualSequence?.IsRunning == true)
                        await TickPagePlaybackAsync();
                    StopPagePlayback();
                    _eventStartedVirtual = false;
                }
                if (_eventStartedSerial)
                {
                    if (_serialSequence?.IsRunning == true)
                        await TickSerialPlaybackAsync();
                    StopSerialPlayback();
                    _eventStartedSerial = false;
                }

                // Existing page playback was never stopped: its next tick
                // automatically resumes the page that the event overlaid.
                if (_virtualSequence?.IsRunning == true)
                    await TickPagePlaybackAsync();
                if (_serialSequence?.IsRunning == true)
                    await TickSerialPlaybackAsync();
                this.FindControl<TextBlock>("EventStatus")!.Text = "No active display events.";
                return;
            }

            if (!_eventTimer.IsEnabled)
                _eventTimer.Start();

            if (_virtualDevice.IsConnected && !_busy)
            {
                if (_virtualSequence?.IsRunning != true)
                {
                    _virtualSequence ??= CreateVirtualSequence();
                    if (_virtualSequence.Start(Array.Empty<PageDefinition>()))
                    {
                        _eventStartedVirtual = true;
                        await _pageTimer.StopAndDrainAsync();
                        _pageTimer.Start();
                        UpdatePagePlaybackControls();
                    }
                }
                if (_virtualSequence?.IsRunning == true)
                    await TickPagePlaybackAsync();
            }

            if (_serialDisplay?.IsConnected == true && !_serialBusy)
            {
                if (_serialSequence?.IsRunning != true)
                {
                    _serialSequence ??= CreateSerialSequence();
                    if (_serialSequence.Start(Array.Empty<PageDefinition>()))
                    {
                        _eventStartedSerial = true;
                        await _serialPlaybackTimer.StopAndDrainAsync();
                        _serialPlaybackTimer.Start();
                        UpdateSerialControls();
                    }
                }
                if (_serialSequence?.IsRunning == true)
                    await TickSerialPlaybackAsync();
            }

            this.FindControl<TextBlock>("EventStatus")!.Text =
                _virtualDevice.IsConnected || _serialDisplay?.IsConnected == true
                    ? "Displaying highest-priority event on connected displays."
                    : "Event queued; connect a virtual or serial LIS2 to display it.";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBlock>("EventStatus")!.Text = "Event display error: " + ex.Message;
        }
        finally
        {
            _updatingEventOverlay = false;
        }
    }
}
