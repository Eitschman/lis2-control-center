using Avalonia.Controls;
using Avalonia.Threading;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private readonly PagePlayback _serialPlayback = new();
    private readonly DispatcherTimer _serialPlaybackTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private bool _serialPlaybackActive;
    private bool _serialPlaybackBusy;
    private string? _lastSerialFrame;

    private void InitializeSerialPlayback()
    {
        _serialPlaybackTimer.Tick += async (_, _) => await TickSerialPlaybackAsync();
        this.FindControl<Button>("SerialPlaybackStart")!.Click += async (_, _) =>
        {
            if (_serialDisplay?.IsConnected != true || _serialBusy || _serialPlaybackActive) return;
            if (SelectedPageIndex is not null && !SavePageEditor()) return;
            _serialPlayback.Reload(_pages.Pages);
            _lastSerialFrame = null;
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
        _lastSerialFrame = null;
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
            if (frame is not null)
            {
                var signature = frame.Line1 + "\n" + frame.Line2;
                if (signature != _lastSerialFrame)
                {
                    await _serialDisplay.RenderBothAsync(frame.Line1, frame.Line2);
                    _lastSerialFrame = signature;
                }
            }
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
