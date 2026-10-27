using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private void InitializePageFormatting()
    {
        foreach (var name in new[] { "FormatVariable", "FormatPrefix", "FormatSuffix", "FormatFallback", "FormatFallbackVariable" })
            this.FindControl<TextBox>(name)!.TextChanged += (_, _) => RefreshPageFormatting();
        foreach (var name in new[] { "FormatKind", "FormatCase" })
            this.FindControl<ComboBox>(name)!.SelectionChanged += (_, _) => RefreshPageFormatting();
        this.FindControl<NumericUpDown>("FormatDecimals")!.ValueChanged += (_, _) => RefreshPageFormatting();
        this.FindControl<Button>("FormatBrowse")!.Click += async (_, _) =>
            await ShowPageVariableBrowserAsync(selectedAction: key => this.FindControl<TextBox>("FormatVariable")!.Text = key);
        this.FindControl<Button>("FormatFallbackBrowse")!.Click += async (_, _) =>
            await ShowPageVariableBrowserAsync(selectedAction: key => this.FindControl<TextBox>("FormatFallbackVariable")!.Text = key);
        this.FindControl<Button>("FormatInsert1")!.Click += (_, _) => InsertFormattedVariable("PageLine1");
        this.FindControl<Button>("FormatInsert2")!.Click += (_, _) => InsertFormattedVariable("PageLine2");
        // Der Hinweis ist per Maus sowie bei fokussiertem Zeilenfeld mit F1 erreichbar.
        foreach (var name in new[] { "PageLine1", "PageLine2" })
        {
            var input = this.FindControl<TextBox>(name)!;
            input.KeyDown += (_, args) =>
            {
                if (args.Key == global::Avalonia.Input.Key.F1)
                {
                    ToolTip.SetIsOpen(input, true);
                    args.Handled = true;
                }
                else if (args.Key == global::Avalonia.Input.Key.Escape && ToolTip.GetIsOpen(input))
                {
                    ToolTip.SetIsOpen(input, false);
                    args.Handled = true;
                }
            };
            input.LostFocus += (_, _) => ToolTip.SetIsOpen(input, false);
        }
        RefreshPageFormatting();
    }

    private void RefreshPageFormatting()
    {
        // Textänderungen können noch nach dem Schließen in der Dispatcherqueue liegen.
        if (_windowClosed) return;
        var kind = this.FindControl<ComboBox>("FormatKind")!.SelectedIndex;
        var digits = this.FindControl<NumericUpDown>("FormatDecimals")!;
        digits.IsEnabled = kind > 0;
        var options = new PageTemplateFormat(this.FindControl<TextBox>("FormatVariable")!.Text ?? "", kind,
            (int)(digits.Value ?? 2), this.FindControl<ComboBox>("FormatCase")!.SelectedIndex,
            this.FindControl<TextBox>("FormatPrefix")!.Text ?? "", this.FindControl<TextBox>("FormatSuffix")!.Text ?? "",
            this.FindControl<TextBox>("FormatFallback")!.Text ?? "", this.FindControl<TextBox>("FormatFallbackVariable")!.Text ?? "");
        var wholeDigits = kind == 0 || digits.Value.HasValue && digits.Value.Value == decimal.Truncate(digits.Value.Value);
        var valid = options.TryBuild(out var template) && wholeDigits;
        this.FindControl<TextBox>("FormatTemplate")!.Text = valid ? template : "";
        var values = PagePreviewValues();
        values.TryGetValue(options.Key, out var value);
        var primaryMissing = LIS2.Display.TemplateRenderer.IsMissingValue(value);
        if (primaryMissing) values.TryGetValue(options.FallbackVariable, out value);
        this.FindControl<TextBlock>("FormatResult")!.Text = valid
            ? new LIS2.Display.TemplateRenderer().Render(template, values) : "";
        SetUiText("FormatStatus", !wholeDigits
            ? "Use a whole number from 0 to 6 for decimal places."
            : !valid
            ? "Choose a variable. Braces and | are not allowed; variable keys cannot contain spaces."
            : LIS2.Display.TemplateRenderer.IsMissingValue(value)
                ? "No current value; the missing-value text is used."
                : primaryMissing ? "The fallback variable is used."
                : "");
        this.FindControl<Button>("FormatInsert1")!.IsEnabled = valid && SelectedPageIndex is not null;
        this.FindControl<Button>("FormatInsert2")!.IsEnabled = valid && SelectedPageIndex is not null;
    }

    private void InsertFormattedVariable(string target)
    {
        RefreshPageFormatting();
        if (!this.FindControl<Button>(target == "PageLine1" ? "FormatInsert1" : "FormatInsert2")!.IsEnabled) return;
        var input = this.FindControl<TextBox>(target)!;
        var old = input.Text ?? "";
        var position = Math.Clamp(input.CaretIndex, 0, old.Length);
        var template = this.FindControl<TextBox>("FormatTemplate")!.Text!;
        input.Text = old.Insert(position, template);
        input.CaretIndex = position + template.Length;
        input.Focus();
        RefreshPagePreview();
    }
}
