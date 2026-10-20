using Avalonia.Controls;
using LIS2.Core;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private bool SerialSelected => _preferredTransportMode == "Serial";
    private IDisplaySession? SelectedDisplay => SerialSelected ? _serialDisplay : _virtualDevice;

    /// <summary>Manuelle Befehle stoppen und leeren nur die Wiedergabe des gewählten Transports.</summary>
    private Task RunSelectedDisplayActionAsync(Func<IDisplaySession, Task> action) => SerialSelected
        ? RunSerialActionAsync(() => action(_serialDisplay ?? throw new InvalidOperationException("Serial LIS2 disconnected.")))
        : RunVirtualActionAsync(() => action(_virtualDevice));

    /// <summary>Gespeicherte Displayeinstellungen gelten für beide Verbindungsarten, ohne Lüfterfreigabe.</summary>
    private async Task ApplyConnectedDisplaySettingsAsync(IDisplaySession session)
    {
        await session.SetBrightnessAsync(BrightnessFromPercent(_preferredBrightness));
        if (this.FindControl<CheckBox>("ProgramGlyphsOnConnect")!.IsChecked == true ||
            (_pages.Pages.Any(page => page.Enabled &&
                (page.Line1Template.Contains("{Winamp.SpectrumGlyphs}", StringComparison.OrdinalIgnoreCase) ||
                 page.Line2Template.Contains("{Winamp.SpectrumGlyphs}", StringComparison.OrdinalIgnoreCase))) &&
             BuiltInGlyphSets.IsSpectrumSet(_glyphs.Glyphs)))
            await session.ProgramAllGlyphsAsync(_glyphs.Glyphs);
    }

    /// <summary>Helligkeitsänderungen werden gespeichert und an den gewählten Transport gesendet.</summary>
    private Task SetSelectedBrightnessAsync(int percent) => RunSelectedDisplayActionAsync(async session =>
    {
        var brightness = BrightnessFromPercent(percent);
        await session.SetBrightnessAsync(brightness);
        _preferredBrightness = percent;
        await SavePortableWorkspaceAsync(true);
    });

    private static Lis2Brightness BrightnessFromPercent(int percent) => percent switch
    {
        100 => Lis2Brightness.Percent100, 75 => Lis2Brightness.Percent75,
        50 => Lis2Brightness.Percent50, 25 => Lis2Brightness.Percent25,
        _ => throw new InvalidOperationException("Invalid brightness setting.")
    };

    /// <summary>Der ROM-Test verwendet dieselben bestätigten Bytes auf beiden Transports.</summary>
    private Task SendSelectedCharacterRomAsync(int start) => RunSelectedDisplayActionAsync(async session =>
    {
        await session.WriteRawDisplayBytesAsync(1, 0, Enumerable.Range(start, 20).Select(value => (byte)value).ToArray());
        await session.WriteRawDisplayBytesAsync(2, 0, Enumerable.Range(start + 20, 20).Select(value => (byte)value).ToArray());
    });
}
