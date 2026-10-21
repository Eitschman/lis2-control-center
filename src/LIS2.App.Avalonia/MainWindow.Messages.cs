using Avalonia.Controls;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private sealed record UiTextBinding(UiMessage Message, string LastText);
    private readonly Dictionary<TextBlock, UiTextBinding> _uiTextBindings = new();

    internal string UiText(string text) => UiTranslations.Translate(text, CurrentUiLanguage());
    private string UiFormat(UiMessage message) => message.Render(CurrentUiLanguage());

    private static UiMessage PagePlaybackName(string? pageId) =>
        pageId is null ? "waiting for visible page" : (UiMessage)$"{pageId}";

    private void SetUiButton(string name, string source)
    {
        var button = this.FindControl<Button>(name)!;
        _originalPageButtons[button] = source;
        button.Content = UiText(source);
    }

    /// <summary>Nur explizit als Oberflächentext registrierte Felder werden beim Sprachwechsel neu geschrieben.</summary>
    private void SetUiText(string name, UiMessage message) => SetUiText(this.FindControl<TextBlock>(name)!, message);
    private void SetUiText(TextBlock control, UiMessage message)
    {
        var text = message.Render(CurrentUiLanguage());
        _uiTextBindings[control] = new(message, text);
        control.Text = text;
    }

    private void RefreshUiMessages()
    {
        foreach (var (control, binding) in _uiTextBindings.ToArray())
        {
            if (control.Text == binding.LastText) SetUiText(control, binding.Message);
            else _uiTextBindings.Remove(control);
        }
    }
}
