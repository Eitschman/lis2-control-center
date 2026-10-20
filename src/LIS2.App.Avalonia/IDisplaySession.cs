using LIS2.Core;

namespace LIS2.App.Avalonia;

/// <summary>Gemeinsame Displaybefehle ohne implizite Freigabe von Lüfterausgängen.</summary>
public interface IDisplaySession : IAsyncDisposable
{
    bool IsConnected { get; }
    Task WriteLineAsync(int line, string text, CancellationToken cancellationToken = default);
    Task RenderBothAsync(string line1, string line2, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task SetBrightnessAsync(Lis2Brightness brightness, CancellationToken cancellationToken = default);
    Task ProgramGlyphAsync(int slot, byte[] rows, CancellationToken cancellationToken = default);
    Task ProgramAllGlyphsAsync(IReadOnlyList<CustomGlyphSettings> glyphs, CancellationToken cancellationToken = default);
    Task WriteRawDisplayBytesAsync(int line, int column, byte[] bytes, CancellationToken cancellationToken = default);
}
