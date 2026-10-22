using Avalonia.Controls;
using System.Globalization;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private bool _rebindingCurvePoints;

    /// <summary>Alle Punktaktionen schreiben in das bestehende Textformat, das auch Export und Runtime verwenden.</summary>
    private void InitializeCurvePoints()
    {
        this.FindControl<ListBox>("FanCurvePointsList")!.SelectionChanged += (_, _) =>
        {
            if (_rebindingCurvePoints) return;
            var index = this.FindControl<ListBox>("FanCurvePointsList")!.SelectedIndex;
            var points = FanCurveEditor.Parse(this.FindControl<TextBox>("FanCurve")!.Text);
            this.FindControl<TextBox>("FanPointTemperature")!.Text = index >= 0 ? points[index].Temperature.ToString("R", CultureInfo.InvariantCulture) : "";
            this.FindControl<TextBox>("FanPointOutput")!.Text = index >= 0 ? points[index].OutputPercent.ToString(CultureInfo.InvariantCulture) : "";
        };
        this.FindControl<Button>("FanPointNew")!.Click += (_, _) =>
        {
            this.FindControl<ListBox>("FanCurvePointsList")!.SelectedIndex = -1;
            this.FindControl<TextBox>("FanPointTemperature")!.Text = "";
            this.FindControl<TextBox>("FanPointOutput")!.Text = "";
        };
        this.FindControl<Button>("FanPointApply")!.Click += (_, _) => EditCurvePoint(false);
        this.FindControl<Button>("FanPointDelete")!.Click += (_, _) => EditCurvePoint(true);
    }

    private void EditCurvePoint(bool delete)
    {
        try
        {
            var points = FanCurveEditor.Parse(this.FindControl<TextBox>("FanCurve")!.Text);
            var selected = this.FindControl<ListBox>("FanCurvePointsList")!.SelectedIndex;
            if (delete && selected < 0) return;
            if (selected >= 0) points.RemoveAt(selected);
            if (!delete)
                points.Add(FanCurveEditor.Parse(this.FindControl<TextBox>("FanPointTemperature")!.Text + "=" +
                    this.FindControl<TextBox>("FanPointOutput")!.Text).Single());
            this.FindControl<TextBox>("FanCurve")!.Text = FanCurveEditor.Format(points.OrderBy(point => point.Temperature));
        }
        catch (FormatException exception) { SetUiText("FanCurveStatus", (UiMessage)exception.Message); }
    }

    private void RefreshCurvePoints()
    {
        _rebindingCurvePoints = true;
        try
        {
            var points = FanCurveEditor.Parse(this.FindControl<TextBox>("FanCurve")!.Text);
            this.FindControl<ListBox>("FanCurvePointsList")!.ItemsSource = points.Select(point =>
                point.Temperature.ToString("R", CultureInfo.InvariantCulture) + " °C → " + point.OutputPercent + " %").ToArray();
            SetUiText("FanCurveStatus", "");
        }
        catch (FormatException exception)
        {
            this.FindControl<ListBox>("FanCurvePointsList")!.ItemsSource = null;
            this.FindControl<global::Avalonia.Controls.Shapes.Path>("FanCurvePreviewPath")!.Data = null;
            SetUiText("FanCurveStatus", (UiMessage)exception.Message);
        }
        finally { _rebindingCurvePoints = false; }
    }
}
