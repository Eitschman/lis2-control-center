using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Display;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private PeriodicRuntimeLoop _serialPlaybackTimer = null!;
    private PageSequenceController? _serialSequence;

    private PageSequenceController CreateSerialSequence() => new(
        (frame, cancellationToken) =>
            (_serialDisplay ?? throw new InvalidOperationException("Serial LIS2 disconnected."))
                .RenderBothAsync(frame.Line1, frame.Line2, cancellationToken),
        cancellationToken =>
            (_serialDisplay ?? throw new InvalidOperationException("Serial LIS2 disconnected."))
                .ClearAsync(cancellationToken));

    private void InitializeSerialPlayback()
    {
        _serialPlaybackTimer = new PeriodicRuntimeLoop(TimeSpan.FromMilliseconds(250),
            _ => { Dispatcher.UIThread.Post(async () =>
            {
                if (_serialSequence?.IsRunning == true) await TickSerialPlaybackAsync();
            }); return Task.CompletedTask; });
        this.FindControl<Button>("SerialPlaybackStart")!.Click += async (_, _) =>
        {
            if (_serialDisplay?.IsConnected != true || _serialBusy || _serialSequence?.IsRunning == true ||
                _serialSequence?.IsBusy == true) return;
            if (SelectedPageIndex is not null && !SavePageEditor()) return;
            _serialSequence ??= CreateSerialSequence();
            if (!_serialSequence.Start(_pages.Pages)) return;
            await _serialPlaybackTimer.StopAndDrainAsync();
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
        _serialSequence?.Stop();
        UpdateSerialControls();
    }

    private void UpdateSerialPlaybackStatus()
    {
        this.FindControl<TextBlock>("SerialPlaybackStatus")!.Text = _serialSequence?.IsRunning == true
            ? $"Serial sequence: {_serialSequence.CurrentPageId ?? "waiting for visible page"}"
            : "Serial sequence stopped";
    }

    private async Task TickSerialPlaybackAsync()
    {
        if (_serialSequence?.IsRunning != true || _serialBusy) return;
        if (_serialDisplay?.IsConnected != true)
        {
            StopSerialPlayback();
            SetSerialStatus("Serial sequence stopped: device disconnected.");
            return;
        }
        try
        {
            await _serialSequence.TickAsync(DateTimeOffset.UtcNow, PagePreviewValues());
            UpdateSerialPlaybackStatus();
        }
        catch (Exception ex)
        {
            StopSerialPlayback();
            SetSerialStatus($"Serial sequence stopped after write error: {ex.Message}");
        }
        finally { UpdateSerialControls(); }
    }
}
