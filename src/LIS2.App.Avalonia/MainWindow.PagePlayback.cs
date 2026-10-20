using Avalonia.Controls;
using Avalonia.Threading;
using LIS2.Display;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private AdaptiveRuntimeLoop _pageTimer = null!;
    private PageSequenceController? _virtualSequence;

    private AdaptiveRuntimeLoop CreatePageTimer() => new(
        async cancellationToken =>
        {
            await UiRuntimeDispatch.RunAsync(
            async () =>
            {
                if (_virtualSequence?.IsRunning == true) await TickPagePlaybackAsync();
            }, cancellationToken);
            return _virtualSequence?.NextDelay ?? TimeSpan.FromSeconds(1);
        });

    private PageSequenceController CreateVirtualSequence() => _runtime.CreateSequence(
        (frame, cancellationToken) => _virtualDevice.RenderBothAsync(frame.Line1, frame.Line2, cancellationToken),
        cancellationToken => _virtualDevice.ClearAsync(cancellationToken));

    private void InitializePagePlayback()
    {
        _pageTimer = CreatePageTimer();
        // The timer runs headlessly; UI and source snapshots stay on the UI thread.

        this.FindControl<Button>("PagePlaybackStart")!.Click += async (_, _) => await StartPagePlaybackAsync();
        this.FindControl<Button>("PagePlaybackStop")!.Click += (_, _) => StopPagePlayback();
        UpdatePagePlaybackControls();
    }

    private async Task StartPagePlaybackAsync()
    {
        if (_virtualSequence?.IsRunning == true || _virtualSequence?.IsBusy == true) return;
        if (!_virtualDevice.IsConnected)
        {
            SetVirtualStatus("Connect the virtual LIS2 before starting the sequence.");
            return;
        }
        if (SelectedPageIndex is not null && !SavePageEditor()) return;
        _virtualSequence ??= CreateVirtualSequence();
        if (!_virtualSequence.Start(_pages.Pages)) return;
        await _pageTimer.StopAndDrainAsync();
        _pageTimer.Start();
        UpdatePagePlaybackControls();
        await TickPagePlaybackAsync();
    }

    private async Task ReloadRunningPagesAsync()
    {
        if (_virtualSequence?.IsRunning != true) return;
        try
        {
            if (await _virtualSequence.ReloadAsync(_pages.Pages))
                await TickPagePlaybackAsync();
        }
        catch (Exception ex)
        {
            StopPagePlayback();
            SetVirtualStatus($"Page sequence reload failed: {ex.Message}");
        }
    }

    private void StopPagePlayback()
    {
        _pageTimer.Stop();
        _virtualSequence?.Stop();
        UpdatePagePlaybackControls();
        UpdateVirtualControls();
    }

    private void UpdatePagePlaybackControls()
    {
        var running = _virtualSequence?.IsRunning == true;
        this.FindControl<Button>("PagePlaybackStart")!.IsEnabled = !running && _virtualSequence?.IsBusy != true;
        this.FindControl<Button>("PagePlaybackStop")!.IsEnabled = running;
        this.FindControl<TextBlock>("PagePlaybackStatus")!.Text = running
            ? $"Sequence running: {_virtualSequence?.CurrentPageId ?? "waiting for visible page"}"
            : "Sequence stopped";
    }

    private async Task TickPagePlaybackAsync()
    {
        if (_virtualSequence?.IsRunning != true || _busy) return;
        if (!_virtualDevice.IsConnected)
        {
            StopPagePlayback();
            return;
        }
        try
        {
            var changed = await _runtime.TickAsync(_virtualSequence);
            if (changed) UpdatePreview();
            UpdatePagePlaybackControls();
        }
        catch (Exception ex)
        {
            StopPagePlayback();
            SetVirtualStatus($"Page sequence stopped: {ex.Message}");
        }
        finally { UpdatePagePlaybackControls(); UpdateVirtualControls(); }
    }
}
