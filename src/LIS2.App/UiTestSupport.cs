using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LIS2.App;

internal sealed class UiRegressionReport
{
    private readonly List<string> _lines = new();

    public int FailureCount { get; private set; }

    public void Info(string message) => _lines.Add($"INFO  {message}");

    public void Pass(string message) => _lines.Add($"PASS  {message}");

    public void Fail(string message)
    {
        FailureCount++;
        _lines.Add($"FAIL  {message}");
    }

    public void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllLines(path, _lines);
    }
}

internal static class UiTestSupport
{
    public static void AssertPositiveSize(
        UiRegressionReport report,
        FrameworkElement element,
        string name)
    {
        if (element.ActualWidth > 0 && element.ActualHeight > 0)
            report.Pass($"{name}: positive size {element.ActualWidth:0.0}x{element.ActualHeight:0.0}");
        else
            report.Fail($"{name}: invalid size {element.ActualWidth:0.0}x{element.ActualHeight:0.0}");
    }

    public static void AssertFullyInside(
        UiRegressionReport report,
        FrameworkElement child,
        FrameworkElement ancestor,
        string name,
        double tolerance = 1.0)
    {
        if (!child.IsVisible)
        {
            report.Fail($"{name}: element is not visible");
            return;
        }

        try
        {
            var topLeft = child.TranslatePoint(new Point(0, 0), ancestor);
            var childRect = new Rect(topLeft, child.RenderSize);
            var ancestorRect = new Rect(new Point(0, 0), ancestor.RenderSize);

            var inside =
                childRect.Left >= ancestorRect.Left - tolerance &&
                childRect.Top >= ancestorRect.Top - tolerance &&
                childRect.Right <= ancestorRect.Right + tolerance &&
                childRect.Bottom <= ancestorRect.Bottom + tolerance;

            if (inside)
            {
                report.Pass(
                    $"{name}: inside container ({childRect.Left:0.0},{childRect.Top:0.0}) " +
                    $"{childRect.Width:0.0}x{childRect.Height:0.0}");
            }
            else
            {
                report.Fail(
                    $"{name}: outside container. child={childRect}; container={ancestorRect}");
            }
        }
        catch (InvalidOperationException ex)
        {
            report.Fail($"{name}: cannot compare visual bounds: {ex.Message}");
        }
    }

    public static void CapturePng(FrameworkElement element, string path)
    {
        element.UpdateLayout();

        var dpi = VisualTreeHelper.GetDpi(element);
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY));

        var bitmap = new RenderTargetBitmap(
            pixelWidth,
            pixelHeight,
            dpi.PixelsPerInchX,
            dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);

        bitmap.Render(element);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    public static string Slug(string value)
    {
        var chars = value
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        return new string(chars)
            .Trim('-')
            .ToLowerInvariant();
    }
}
