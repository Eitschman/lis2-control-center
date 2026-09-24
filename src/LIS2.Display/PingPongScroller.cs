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

    public string Render(string? value, DateTimeOffset now)
    {
        var safe = Lis2Protocol.ToSafeAscii(value ?? string.Empty);

        if (!string.Equals(_text, safe, StringComparison.Ordinal))
            Reset(safe, now);

        if (_text.Length <= DisplayFrame.Width)
            return DisplayFrame.Normalize(_text);

        AdvanceUntil(now);

        return _text.Substring(_offset, DisplayFrame.Width);
    }

    public TimeSpan TimeUntilNextChange(DateTimeOffset now)
    {
        if (!IsScrolling)
            return Timeout.InfiniteTimeSpan;

        var remaining = _nextChangeAt - now;
        return remaining <= TimeSpan.Zero
            ? TimeSpan.FromMilliseconds(1)
            : remaining;
    }

    public void Reset(string? value, DateTimeOffset now)
    {
        _text = Lis2Protocol.ToSafeAscii(value ?? string.Empty);
        _offset = 0;
        _direction = 1;
        _nextChangeAt = _text.Length > DisplayFrame.Width
            ? now + EdgePause
            : DateTimeOffset.MaxValue;
    }

    private void AdvanceUntil(DateTimeOffset now)
    {
        var maxOffset = _text.Length - DisplayFrame.Width;

        while (now >= _nextChangeAt)
        {
            _offset += _direction;

            if (_offset >= maxOffset)
            {
                _offset = maxOffset;
                _direction = -1;
                _nextChangeAt += EdgePause;
            }
            else if (_offset <= 0)
            {
                _offset = 0;
                _direction = 1;
                _nextChangeAt += EdgePause;
            }
            else
            {
                _nextChangeAt += StepInterval;
            }
        }
    }
}
