using LIS2.Display;

namespace LIS2.Display.Tests;

public sealed class PingPongScrollerTests
{
    [Fact]
    public void ShortText_DoesNotScroll()
    {
        var scroller = new PingPongScroller();
        var now = DateTimeOffset.UtcNow;

        var first = scroller.Render("HELLO", now);
        var later = scroller.Render("HELLO", now.AddSeconds(5));

        Assert.Equal("HELLO".PadRight(DisplayFrame.Width), first);
        Assert.Equal(first, later);
        Assert.False(scroller.IsScrolling);
    }

    [Fact]
    public void LongText_PausesThenMovesForwardAndBack()
    {
        var scroller = new PingPongScroller();
        var now = DateTimeOffset.UtcNow;
        const string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        Assert.Equal("ABCDEFGHIJKLMNOPQRST", scroller.Render(text, now));
        Assert.Equal(
            "ABCDEFGHIJKLMNOPQRST",
            scroller.Render(text, now.AddMilliseconds(899)));

        Assert.Equal(
            "BCDEFGHIJKLMNOPQRSTU",
            scroller.Render(text, now.AddMilliseconds(900)));

        Assert.Equal(
            "GHIJKLMNOPQRSTUVWXYZ",
            scroller.Render(text, now.AddMilliseconds(2400)));

        Assert.Equal(
            "GHIJKLMNOPQRSTUVWXYZ",
            scroller.Render(text, now.AddMilliseconds(3299)));

        Assert.Equal(
            "FGHIJKLMNOPQRSTUVWXY",
            scroller.Render(text, now.AddMilliseconds(3300)));
    }

    [Fact]
    public void ChangedText_RestartsAtLeftEdge()
    {
        var scroller = new PingPongScroller();
        var now = DateTimeOffset.UtcNow;

        scroller.Render("ABCDEFGHIJKLMNOPQRSTUVWXYZ", now);
        scroller.Render("ABCDEFGHIJKLMNOPQRSTUVWXYZ", now.AddSeconds(2));

        var changed = scroller.Render(
            "012345678901234567890123456789",
            now.AddSeconds(2));

        Assert.Equal("01234567890123456789", changed);
    }

    [Fact]
    public void NativeGermanCharacters_UseOneCellWhileScrolling()
    {
        var scroller = new PingPongScroller();
        var now = DateTimeOffset.UtcNow;

        var frame = scroller.Render("123456789012345678äXYZ", now);

        Assert.Equal("123456789012345678äX", frame);
        Assert.True(scroller.IsScrolling);
    }
    [Fact]
    public void CustomGlyphCell_IsPreservedWhileScrolling()
    {
        var scroller = new PingPongScroller();
        var now = DateTimeOffset.UtcNow;
        var glyph = LIS2.Core.Lis2Protocol.CustomGlyph(2);
        var text = $"1234567890123456789{glyph}XYZ";

        var frame = scroller.Render(text, now);

        Assert.Equal(glyph, frame[19]);
        Assert.True(scroller.IsScrolling);
    }
}
