using System.Numerics;

namespace LIS2.WmpLegacy.Audio;

/// <summary>Stereo-Spitzenpegel und zwanzig logarithmische Frequenzbänder aus PCM16, ohne Audiodateien.</summary>
public sealed class SignalAnalyzer
{
    public const int FftSize = 2048;
    private readonly double[] _left = new double[FftSize];
    private readonly double[] _right = new double[FftSize];
    private int _position, _count, _leftPeak, _rightPeak;
    private long _frames;

    public void Add(short[] interleaved)
    {
        if (interleaved.Length % 2 != 0) throw new ArgumentException("Stereo frames must contain two samples.", nameof(interleaved));
        for (var offset = 0; offset < interleaved.Length; offset += 2)
        {
            var left = (int)interleaved[offset];
            var right = (int)interleaved[offset + 1];
            _leftPeak = Math.Max(_leftPeak, Math.Abs(left));
            _rightPeak = Math.Max(_rightPeak, Math.Abs(right));
            // Getrennte Kanäle verhindern Auslöschung und Verzerrungen beim Stereo-Mischen.
            _left[_position] = left / 32768.0;
            _right[_position] = right / 32768.0;
            _position = (_position + 1) % FftSize;
            _count = Math.Min(_count + 1, FftSize);
            _frames++;
        }
    }

    public AudioLevels ReadAndResetPeaks()
    {
        var spectrum = new int[20];
        if (_frames > 0 && _count == FftSize)
        {
            var leftValues = new Complex[FftSize];
            var rightValues = new Complex[FftSize];
            for (var index = 0; index < FftSize; index++)
            {
                var window = 0.5 - 0.5 * Math.Cos(2 * Math.PI * index / (FftSize - 1));
                leftValues[index] = _left[(_position + index) % FftSize] * window;
                rightValues[index] = _right[(_position + index) % FftSize] * window;
            }
            Transform(leftValues);
            Transform(rightValues);
            for (var band = 0; band < 20; band++)
            {
                var low = 40 * Math.Pow(500, band / 20.0);
                var high = 40 * Math.Pow(500, (band + 1) / 20.0);
                var begin = Math.Max(1, (int)Math.Ceiling(low * FftSize / 44100));
                var end = Math.Min(FftSize / 2, (int)Math.Ceiling(high * FftSize / 44100));
                var magnitude = 0.0;
                for (var bin = begin; bin < end; bin++)
                    magnitude = Math.Max(magnitude,
                        Math.Max(leftValues[bin].Magnitude, rightValues[bin].Magnitude) * 4 / FftSize);
                var decibels = 20 * Math.Log10(Math.Max(1e-6, magnitude));
                spectrum[band] = (int)Math.Round(Math.Clamp((decibels + 60) / 60, 0, 1) * 255);
            }
        }
        var result = new AudioLevels(_frames, Math.Min(255, _leftPeak * 255 / 32768),
            Math.Min(255, _rightPeak * 255 / 32768), spectrum);
        _leftPeak = _rightPeak = 0;
        _frames = 0;
        return result;
    }

    private static void Transform(Complex[] values)
    {
        for (int index = 1, reverse = 0; index < values.Length; index++)
        {
            var bit = values.Length >> 1;
            while ((reverse & bit) != 0) { reverse ^= bit; bit >>= 1; }
            reverse ^= bit;
            if (index < reverse) (values[index], values[reverse]) = (values[reverse], values[index]);
        }
        for (var length = 2; length <= values.Length; length *= 2)
        {
            var step = Complex.FromPolarCoordinates(1, -2 * Math.PI / length);
            for (var offset = 0; offset < values.Length; offset += length)
            {
                var factor = Complex.One;
                for (var index = 0; index < length / 2; index++)
                {
                    var even = values[offset + index];
                    var odd = factor * values[offset + index + length / 2];
                    values[offset + index] = even + odd;
                    values[offset + index + length / 2] = even - odd;
                    factor *= step;
                }
            }
        }
    }
}

public sealed record AudioLevels(long Frames, int VuLeft, int VuRight, int[] Spectrum);
