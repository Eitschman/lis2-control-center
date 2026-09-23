using LIS2.Core;

namespace LIS2.Display;

public sealed class DisplayFrameWriter
{
    private readonly ILis2Device _device;
    private DisplayFrame? _lastFrame;

    public DisplayFrameWriter(ILis2Device device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public DisplayFrame? LastFrame => _lastFrame;

    public async Task WriteAsync(
        DisplayFrame frame,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (_lastFrame is null)
        {
            await _device.WriteLineAsync(1, frame.Line1, cancellationToken)
                .ConfigureAwait(false);
            await _device.WriteLineAsync(2, frame.Line2, cancellationToken)
                .ConfigureAwait(false);

            _lastFrame = frame;
            return;
        }

        await WriteChangedSpansAsync(
            1,
            _lastFrame.Line1,
            frame.Line1,
            cancellationToken).ConfigureAwait(false);

        await WriteChangedSpansAsync(
            2,
            _lastFrame.Line2,
            frame.Line2,
            cancellationToken).ConfigureAwait(false);

        _lastFrame = frame;
    }

    public void Reset() => _lastFrame = null;

    private async Task WriteChangedSpansAsync(
        int line,
        string previous,
        string current,
        CancellationToken cancellationToken)
    {
        var index = 0;

        while (index < DisplayFrame.Width)
        {
            while (index < DisplayFrame.Width &&
                   previous[index] == current[index])
            {
                index++;
            }

            if (index >= DisplayFrame.Width)
                break;

            var start = index;

            while (index < DisplayFrame.Width &&
                   previous[index] != current[index])
            {
                index++;
            }

            var length = index - start;
            var changed = current.Substring(start, length);

            await _device.WriteAsync(
                line,
                start,
                changed,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
