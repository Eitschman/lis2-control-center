using LIS2.WmpLegacy.Audio;

namespace LIS2.WmpLegacy.Tests;

public sealed class SignalAnalyzerTests
{
    [Fact]
    public void SilenceAndMissingPacketsDoNotKeepOldLevels()
    {
        var analyzer = new SignalAnalyzer();
        analyzer.Add(Tone(1000, 16000, 8000));
        Assert.True(analyzer.ReadAndResetPeaks().Spectrum.Max() > 150);
        var missing = analyzer.ReadAndResetPeaks();
        Assert.Equal(0, missing.Frames);
        Assert.Equal(0, missing.VuLeft);
        Assert.All(missing.Spectrum, level => Assert.Equal(0, level));
        analyzer.Add(new short[SignalAnalyzer.FftSize * 2]);
        var silent = analyzer.ReadAndResetPeaks();
        Assert.Equal(SignalAnalyzer.FftSize, silent.Frames);
        Assert.Equal(0, silent.VuLeft);
        Assert.Equal(0, silent.VuRight);
        Assert.All(silent.Spectrum, level => Assert.Equal(0, level));
    }

    [Theory]
    [InlineData(440, 16000, 0)]
    [InlineData(1000, 0, 16000)]
    [InlineData(4000, 16000, -16000)]
    public void ToneHasExpectedFrequencyBandAndIndependentStereoPeaks(int frequency, int left, int right)
    {
        var analyzer = new SignalAnalyzer();
        analyzer.Add(Tone(frequency, left, right));
        var levels = analyzer.ReadAndResetPeaks();
        Assert.InRange(levels.VuLeft, Math.Abs(left) * 255 / 32768 - 1, Math.Abs(left) * 255 / 32768 + 1);
        Assert.InRange(levels.VuRight, Math.Abs(right) * 255 / 32768 - 1, Math.Abs(right) * 255 / 32768 + 1);
        var expected = (int)Math.Floor(Math.Log(frequency / 40.0) / Math.Log(500) * 20);
        Assert.Equal(expected, Array.IndexOf(levels.Spectrum, levels.Spectrum.Max()));
        Assert.InRange(levels.Spectrum[expected], 200, 255);
    }

    [Fact]
    public void FullScaleNegativeSampleDoesNotOverflowAndOddPacketIsRejected()
    {
        var analyzer = new SignalAnalyzer();
        Assert.Throws<ArgumentException>(() => analyzer.Add([0]));
        analyzer.Add([short.MinValue, short.MaxValue]);
        var levels = analyzer.ReadAndResetPeaks();
        Assert.Equal(255, levels.VuLeft);
        Assert.InRange(levels.VuRight, 254, 255);
    }

    [Fact]
    public void PacketBoundariesDoNotChangeTheSpectrum()
    {
        var tone = Tone(1000, 12000, 6000);
        var whole = new SignalAnalyzer();
        whole.Add(tone);
        var chunks = new SignalAnalyzer();
        foreach (var chunk in tone.Chunk(128)) chunks.Add(chunk);
        var expected = whole.ReadAndResetPeaks();
        var actual = chunks.ReadAndResetPeaks();
        Assert.Equal(expected.Spectrum, actual.Spectrum);
        Assert.Equal(expected.VuLeft, actual.VuLeft);
        Assert.Equal(expected.Frames, actual.Frames);
    }

    private static short[] Tone(int frequency, int left, int right)
    {
        var samples = new short[SignalAnalyzer.FftSize * 2];
        for (var frame = 0; frame < SignalAnalyzer.FftSize; frame++)
        {
            var signal = Math.Sin(2 * Math.PI * frequency * frame / 44100);
            samples[frame * 2] = (short)(signal * left);
            samples[frame * 2 + 1] = (short)(signal * right);
        }
        return samples;
    }
}
