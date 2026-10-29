using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Globalization;

namespace LIS2.App.Avalonia;

/// <summary>Drei Einheiten mit gemeinsamer Zeitachse; fehlende oder verspätete Messungen werden nicht verbunden.</summary>
public sealed class FanHistoryPlot : Control
{
    public static readonly StyledProperty<IBrush> AxisBrushProperty =
        AvaloniaProperty.Register<FanHistoryPlot, IBrush>(nameof(AxisBrush), Brushes.Gray);
    public IBrush AxisBrush { get => GetValue(AxisBrushProperty); set => SetValue(AxisBrushProperty, value); }
    static FanHistoryPlot() => AffectsRender<FanHistoryPlot>(AxisBrushProperty);
    private FanHistorySample[] _samples = [];
    private DateTimeOffset _end;
    private TimeSpan _window = TimeSpan.FromMinutes(5);
    internal void Update(FanHistorySample[] samples, DateTimeOffset end, TimeSpan window)
    {
        _samples = samples; _end = end; _window = window; InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var foreground = AxisBrush;
        var width = Math.Max(1, Bounds.Width - 62);
        var height = (Bounds.Height - 26) / 3;
        var colors = new[] { Brushes.DodgerBlue, Brushes.MediumSeaGreen, Brushes.DarkOrange };
        for (var lane = 0; lane < 3; lane++)
        {
            double? Value(FanHistorySample sample) => lane switch { 0 => sample.Sensor, 1 => sample.Target, _ => sample.Rpm };
            var valid = _samples.Select(Value).Where(value => value.HasValue).Select(value => value!.Value).ToArray();
            var minimum = lane == 0 && valid.Length > 0 ? Math.Min(0, valid.Min()) : 0;
            var maximum = lane == 1 ? 100 : valid.Length > 0 ? Math.Max(minimum + 1, valid.Max() * 1.1) : 1;
            var top = lane * height + 10;
            var bottom = top + height - 22;
            context.DrawLine(new Pen(Brushes.Gray, 0.5), new Point(52, bottom), new Point(width + 52, bottom));
            context.DrawText(Text(maximum.ToString("0.#", CultureInfo.CurrentCulture), foreground), new Point(0, top));
            context.DrawText(Text(minimum.ToString("0.#", CultureInfo.CurrentCulture), foreground), new Point(0, bottom - 12));
            Point? previous = null;
            DateTimeOffset? previousTime = null;
            foreach (var sample in _samples)
            {
                var value = Value(sample);
                if (!value.HasValue) { previous = null; previousTime = null; continue; }
                var point = new Point(52 + (sample.Time - (_end - _window)).TotalSeconds / _window.TotalSeconds * width,
                    bottom - (value.Value - minimum) / (maximum - minimum) * (bottom - top));
                if (previous.HasValue && sample.Time - previousTime!.Value <= TimeSpan.FromSeconds(5))
                    context.DrawLine(new Pen(colors[lane], 1.8), previous.Value, point);
                else context.DrawEllipse(colors[lane], null, point, 2, 2);
                previous = point; previousTime = sample.Time;
            }
        }
        context.DrawText(Text((_end - _window).ToLocalTime().ToString("T", CultureInfo.CurrentCulture), foreground), new Point(52, Bounds.Height - 20));
        var endText = Text(_end.ToLocalTime().ToString("T", CultureInfo.CurrentCulture), foreground);
        context.DrawText(endText, new Point(Math.Max(52, Bounds.Width - endText.Width), Bounds.Height - 20));
    }

    private static FormattedText Text(string value, IBrush foreground) => new(value, CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight, new Typeface(FontFamily.Default), 11, foreground);
}
