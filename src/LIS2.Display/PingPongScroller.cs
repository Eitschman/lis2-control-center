using LIS2.Core;

namespace LIS2.Display;

public sealed class PingPongScroller
{
    public static readonly TimeSpan StepInterval = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan EdgePause = TimeSpan.FromMilliseconds(900);

    private string _text = string.Empty;
    private int _offset;
    private int _direction = 1;
    private DateTimeOffset _nextChangeAt = DateTimeOffset.MinValue;

    public bool IsScrolling => _text.Length > DisplayFrame.Width;

    public string Render(
        string? value,
        DateTimeOffset now,
        DisplayOverflowMode mode = DisplayOverflowMode.PingPong,
        TimeSpan? stepInterval = null,
        TimeSpan? edgePause = null)
    {
        var safe = Lis2Protocol.ToSafeAscii(value ?? string.Empty);
        var step = Normalize(stepInterval, StepInterval);
        var pause = Normalize(edgePause, EdgePause);

        if (!string.Equals(_text, safe, StringComparison.Ordinal))
            Reset(safe, now, pause);

        if (_text.Length <= DisplayFrame.Width)
            return DisplayFrame.Normalize(_text);

        if (mode == DisplayOverflowMode.Truncate)
            return _text[..DisplayFrame.Width];

        AdvanceUntil(now, mode, step, pause);

        return _text.Substring(_offset, DisplayFrame.Width);
    }

    public TimeSpan TimeUntilNextChange(
        DateTimeOffset now,
        DisplayOverflowMode mode = DisplayOverflowMode.PingPong)
    {
        if (!IsScrolling || mode == DisplayOverflowMode.Truncate)
            return Timeout.InfiniteTimeSpan;

        var remaining = _nextChangeAt - now;
        return remaining <= TimeSpan.Zero
            ? TimeSpan.FromMilliseconds(1)
            : remaining;
    }

    public void Reset(string? value, DateTimeOffset now, TimeSpan? edgePause = null)
    {
        _text = Lis2Protocol.ToSafeAscii(value ?? string.Empty);
        _offset = 0;
        _direction = 1;
        _nextChangeAt = _text.Length > DisplayFrame.Width
            ? now + Normalize(edgePause, EdgePause)
            : DateTimeOffset.MaxValue;
    }

    private void AdvanceUntil(
        DateTimeOffset now,
        DisplayOverflowMode mode,
        TimeSpan stepInterval,
        TimeSpan edgePause)
    {
        var maxOffset = _text.Length - DisplayFrame.Width;

        while (now >= _nextChangeAt)
        {
            if (mode == DisplayOverflowMode.Marquee)
            {
                _offset++;
                if (_offset > maxOffset)
                {
                    _offset = 0;
                    _nextChangeAt += edgePause;
                }
                else
                {
                    _nextChangeAt += stepInterval;
                }

                continue;
            }

            _offset += _direction;

            if (_offset >= maxOffset)
            {
                _offset = maxOffset;
                _direction = -1;
                _nextChangeAt += edgePause;
            }
            else if (_offset <= 0)
            {
                _offset = 0;
                _direction = 1;
                _nextChangeAt += edgePause;
            }
            else
            {
                _nextChangeAt += stepInterval;
            }
        }
    }

    private static TimeSpan Normalize(TimeSpan? value, TimeSpan fallback) =>
        value is null || value <= TimeSpan.Zero ? fallback : value.Value;
}
