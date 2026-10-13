using Avalonia.Controls;
using Avalonia.Threading;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PagePlayback _serialPlayback = new();
    private readonly DispatcherTimer _serialPlaybackTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private bool _serialPlaybackActive;
    private bool _serialPlaybackBusy;
    private PageFrameDispatcher? _serialFrameDispatcher;

    private PageFrameDispatcher CreateSerialFrameDispatcher() => new(
        async (frame, cancellationToken) =>
        {
            var session = _serialDisplay ?? throw new InvalidOperationException("Serial LIS2 disconnected.");
            await session.RenderBothAsync(frame.Line1, frame.Line2, cancellationToken);
        },
        cancellationToken => (_serialDisplay ?? throw new InvalidOperationException("Serial LIS2 disconnected."))
            .ClearAsync(cancellationToken));

    private void InitializeSerialPlayback()
    {
        _serialPlaybackTimer.Tick += async (_, _) => await TickSerialPlaybackAsync();
        this.FindControl<Button>("SerialPlaybackStart")!.Click += async (_, _) =>
        {
            if (_serialDisplay?.IsConnected != true || _serialBusy || _serialPlaybackActive) return;
            if (SelectedPageIndex is not null && !SavePageEditor()) return;
            _serialPlayback.Reload(_pages.Pages);
            _serialFrameDispatcher = CreateSerialFrameDispatcher();
            _serialPlaybackActive = true;
            _serialPlaybackTimer.Start();
            UpdateSerialControls();
            await TickSerialPlaybackAsync();
        };
        this.FindControl<Button>("SerialPlaybackStop")!.Click += (_, _) => StopSerialPlayback();
        UpdateSerialPlaybackStatus();
    }

    private void StopSerialPlayback()
    {
        _serialPlaybackTimer.Stop();
        _serialPlaybackActive = false;
        UpdateSerialControls();
    }

    private void UpdateSerialPlaybackStatus()
    {
        this.FindControl<TextBlock>("SerialPlaybackStatus")!.Text = _serialPlaybackActive
            ? $"Serial sequence: {_serialPlayback.CurrentPageId ?? "waiting for visible page"}"
            : "Serial sequence stopped";
    }

    private async Task TickSerialPlaybackAsync()
    {
        if (!_serialPlaybackActive || _serialPlaybackBusy || _serialBusy) return;
        if (_serialDisplay?.IsConnected != true)
        {
            StopSerialPlayback();
            SetSerialStatus("Serial sequence stopped: device disconnected.");
            return;
        }

        _serialPlaybackBusy = true;
        try
        {
            var frame = _serialPlayback.Tick(DateTimeOffset.UtcNow, PagePreviewValues());
            await (_serialFrameDispatcher ?? throw new InvalidOperationException("Serial playback not initialized.")).DispatchAsync(frame);
            UpdateSerialPlaybackStatus();
        }
        catch (Exception ex)
        {
            StopSerialPlayback();
            SetSerialStatus($"Serial sequence stopped after write error: {ex.Message}");
        }
        finally
        {
            _serialPlaybackBusy = false;
            UpdateSerialControls();
        }
    }
}
