using Avalonia.Controls;
using Avalonia.Threading;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PagePlayback _pagePlayback = new();
    private readonly DispatcherTimer _pageTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private bool _pagePlaybackActive;
    private bool _pageTickBusy;
    private string? _lastPageFrame;

    private void InitializePagePlayback()
    {
        _pageTimer.Tick += async (_, _) => await TickPagePlaybackAsync();
        this.FindControl<Button>("PagePlaybackStart")!.Click += async (_, _) =>
        {
            if (!_virtualDevice.IsConnected)
            {
                SetVirtualStatus("Connect the virtual LIS2 before starting the sequence.");
                return;
            }
            if (SelectedPageIndex is not null && !SavePageEditor())
                return;
            _pagePlayback.Reload(_pages.Pages);
            _lastPageFrame = null;
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
        _lastPageFrame = null;
        UpdatePagePlaybackControls();
    }

    private void UpdatePagePlaybackControls()
    {
        this.FindControl<Button>("PagePlaybackStart")!.IsEnabled = !_pagePlaybackActive;
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
            if (frame is null)
            {
                UpdatePagePlaybackControls();
                return;
            }
            var signature = frame.Line1 + "\n" + frame.Line2;
            if (signature != _lastPageFrame)
            {
                await _virtualDevice.RenderBothAsync(frame.Line1, frame.Line2);
                _lastPageFrame = signature;
                UpdatePreview();
            }
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
