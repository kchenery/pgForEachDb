using Avalonia;
using Avalonia.Data;
using AvaloniaEdit;

namespace ForEachDb.Desktop.Behaviors;

/// <summary>
/// Two-way bindable text for AvaloniaEdit's TextEditor (the built-in Text property is not a styled property).
/// </summary>
public static class TextEditorBindings
{
    public static readonly AttachedProperty<string?> BoundTextProperty =
        AvaloniaProperty.RegisterAttached<TextEditor, string?>(
            "BoundText",
            typeof(TextEditorBindings));

    private static readonly AttachedProperty<bool> HookedProperty =
        AvaloniaProperty.RegisterAttached<TextEditor, bool>("__Hooked", typeof(TextEditorBindings));

    static TextEditorBindings()
    {
        BoundTextProperty.Changed.AddClassHandler<TextEditor>(OnBoundTextChanged);
    }

    public static string? GetBoundText(TextEditor editor) => editor.GetValue(BoundTextProperty);
    public static void SetBoundText(TextEditor editor, string? value) => editor.SetValue(BoundTextProperty, value);

    private static void OnBoundTextChanged(TextEditor editor, AvaloniaPropertyChangedEventArgs e)
    {
        if (!editor.GetValue(HookedProperty))
        {
            editor.SetValue(HookedProperty, true);
            editor.TextChanged += (_, _) => SetBoundText(editor, editor.Document.Text);
        }

        var value = e.NewValue as string ?? string.Empty;
        if (editor.Document.Text != value)
            editor.Document.Text = value;
    }
}
