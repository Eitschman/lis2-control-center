using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private void InitializePageVariables()
    {
        this.FindControl<Button>("PageRefreshVariables")!.Click += (_, _) => RefreshPageVariables();
        this.FindControl<Button>("PageInsertLine1")!.Click += (_, _) => InsertPageVariable("PageLine1");
        this.FindControl<Button>("PageInsertLine2")!.Click += (_, _) => InsertPageVariable("PageLine2");
        RefreshPageVariables();
    }

    private void RefreshPageVariables()
    {
        var names = PagePreviewValues().Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        var picker = this.FindControl<ComboBox>("PageVariablePicker")!;
        var selected = picker.SelectedItem as string;
        picker.ItemsSource = names;
        picker.SelectedItem = selected is not null && names.Contains(selected) ? selected : names.FirstOrDefault();
        this.FindControl<TextBlock>("PageVariableStatus")!.Text =
            $"{names.Length} variables available (Clock from original Windows source; other providers pending).";
        RefreshPagePreview();
    }

    private void InsertPageVariable(string target)
    {
        if (this.FindControl<ComboBox>("PageVariablePicker")!.SelectedItem is not string name)
            return;
        var input = this.FindControl<TextBox>(target)!;
        var old = input.Text ?? string.Empty;
        var position = Math.Clamp(input.CaretIndex, 0, old.Length);
        var token = "{" + name + "}";
        input.Text = old.Insert(position, token);
        input.CaretIndex = position + token.Length;
        input.Focus();
        RefreshPagePreview();
    }
}
