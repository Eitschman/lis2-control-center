using Avalonia.Controls;
using Avalonia.Threading;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PagePlayback _pagePlayback = new();
    private readonly DispatcherTimer _pageTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private bool _pagePlaybackActive;
    private bool _pageTickBusy;
    private PageFrameDispatcher? _virtualFrameDispatcher;

    private PageFrameDispatcher CreateVirtualFrameDispatcher() => new(
        async (frame, cancellationToken) =>
            await _virtualDevice.RenderBothAsync(frame.Line1, frame.Line2, cancellationToken),
        cancellationToken => _virtualDevice.ClearAsync(cancellationToken));

    private void InitializePagePlayback()
    {
        _pageTimer.Tick += async (_, _) => await TickPagePlaybackAsync();
        this.FindControl<Button>("PagePlaybackStart")!.Click += async (_, _) =>
        {
            if (_pageTickBusy || _pagePlaybackActive) return;
            if (!_virtualDevice.IsConnected)
            {
                SetVirtualStatus("Connect the virtual LIS2 before starting the sequence.");
                return;
            }
            if (SelectedPageIndex is not null && !SavePageEditor())
                return;
            _pagePlayback.Reload(_pages.Pages);
            _virtualFrameDispatcher = CreateVirtualFrameDispatcher();
            _pagePlaybackActive = true;
            _pageTimer.Start();
            UpdatePagePlaybackControls();
            await TickPagePlaybackAsync();
        };
        this.FindControl<Button>("PagePlaybackStop")!.Click += (_, _) => StopPagePlayback();
        UpdatePagePlaybackControls();
    }

    private void StopPagePlayback()
    {
        _pageTimer.Stop();
        _pagePlaybackActive = false;
        UpdatePagePlaybackControls();
    }

    private void UpdatePagePlaybackControls()
    {
        this.FindControl<Button>("PagePlaybackStart")!.IsEnabled = !_pagePlaybackActive && !_pageTickBusy;
        this.FindControl<Button>("PagePlaybackStop")!.IsEnabled = _pagePlaybackActive;
        this.FindControl<TextBlock>("PagePlaybackStatus")!.Text = _pagePlaybackActive
            ? $"Sequence running: {_pagePlayback.CurrentPageId ?? "waiting for visible page"}"
            : "Sequence stopped";
    }

    private async Task TickPagePlaybackAsync()
    {
        if (!_pagePlaybackActive || _pageTickBusy || _busy || !_virtualDevice.IsConnected)
        {
            if (_pagePlaybackActive && !_virtualDevice.IsConnected)
                StopPagePlayback();
            return;
        }
        _pageTickBusy = true;
        try
        {
            var frame = _pagePlayback.Tick(DateTimeOffset.UtcNow, PagePreviewValues());
            var changed = await (_virtualFrameDispatcher ?? throw new InvalidOperationException("Virtual playback not initialized.")).DispatchAsync(frame);
            if (changed) UpdatePreview();
            UpdatePagePlaybackControls();
        }
        catch (Exception ex)
        {
            StopPagePlayback();
            SetVirtualStatus($"Page sequence stopped: {ex.Message}");
        }
        finally { _pageTickBusy = false; }
    }
}
