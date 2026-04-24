using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ForEachDb.Console.ViewModels;

namespace ForEachDb.Console;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // iTerm2 / many terminals send DEL (0x7f) for the Delete/Backspace key, which
    // Consolonia surfaces as Key.Delete. TextBox only removes the char before the
    // cursor on Key.Back, so we remap here so backspace edits work in every field.
    private static void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete) return;
        if (e.KeyModifiers != KeyModifiers.None) return;
        if (e.Source is not TextBox textBox) return;

        var text = textBox.Text ?? string.Empty;
        var start = textBox.SelectionStart;
        var end = textBox.SelectionEnd;
        var caret = textBox.CaretIndex;

        if (start != end)
        {
            var from = Math.Min(start, end);
            var to = Math.Max(start, end);
            textBox.Text = text.Remove(from, to - from);
            textBox.CaretIndex = from;
            textBox.SelectionStart = textBox.SelectionEnd = from;
        }
        else if (caret > 0)
        {
            textBox.Text = text.Remove(caret - 1, 1);
            textBox.CaretIndex = caret - 1;
            textBox.SelectionStart = textBox.SelectionEnd = caret - 1;
        }

        e.Handled = true;
    }
}
