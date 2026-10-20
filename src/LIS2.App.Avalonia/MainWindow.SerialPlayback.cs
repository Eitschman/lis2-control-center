using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Display;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private AdaptiveRuntimeLoop _serialPlaybackTimer = null!;
    private PageSequenceController? _serialSequence;

    private PageSequenceController CreateSerialSequence() => _runtime.CreateSequence(
        (frame, cancellationToken) =>
            (_serialDisplay ?? throw new InvalidOperationException("Serial LIS2 disconnected."))
                .RenderBothAsync(frame.Line1, frame.Line2, cancellationToken),
        cancellationToken =>
            (_serialDisplay ?? throw new InvalidOperationException("Serial LIS2 disconnected."))
                .ClearAsync(cancellationToken));

    private void InitializeSerialPlayback()
    {
        _serialPlaybackTimer = new AdaptiveRuntimeLoop(
            async cancellationToken =>
            {
                await UiRuntimeDispatch.RunAsync(
                async () =>
                {
                    if (_serialSequence?.IsRunning == true) await TickSerialPlaybackAsync();
                }, cancellationToken);
                return _serialSequence?.NextDelay ?? TimeSpan.FromSeconds(1);
            });
        this.FindControl<Button>("SerialPlaybackStart")!.Click += async (_, _) => await StartSerialPlaybackAsync();
        this.FindControl<Button>("SerialPlaybackStop")!.Click += (_, _) => StopSerialPlayback();
        UpdateSerialPlaybackStatus();
    }

    /// <summary>Startet die serielle Seitenfolge auch über die gemeinsame Gerätebedienung.</summary>
    private async Task StartSerialPlaybackAsync()
    {
        if (_serialDisplay?.IsConnected != true || _serialBusy || _serialSequence?.IsBusy == true ||
            (_serialSequence?.IsRunning == true && !_eventStartedSerial)) return;
        if (SelectedPageIndex is not null && !SavePageEditor()) return;
        _serialSequence ??= CreateSerialSequence();
        if (_eventStartedSerial && _serialSequence.IsRunning)
        {
            // Preserve the active event while promoting temporary playback.
            if (!await _serialSequence.ReloadAsync(_pages.Pages)) return;
            _eventStartedSerial = false;
            UpdateSerialControls();
            await TickSerialPlaybackAsync();
            return;
        }
        _eventStartedSerial = false;
        if (!_serialSequence.Start(_pages.Pages)) return;
        await _serialPlaybackTimer.StopAndDrainAsync();
        _serialPlaybackTimer.Start();
        UpdateSerialControls();
        await TickSerialPlaybackAsync();
    }

    private void StopSerialPlayback()
    {
        _serialPlaybackTimer.Stop();
        _serialSequence?.Stop();
        _eventStartedSerial = false;
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
            await _runtime.TickAsync(_serialSequence);
            UpdatePreview();
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
