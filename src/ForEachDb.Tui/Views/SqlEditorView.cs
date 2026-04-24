using ForEachDb.Tui.Infrastructure;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public sealed class SqlEditorView : FrameView
{
    private readonly TextView _text;
    private readonly SqlHistory _history = new();

    public SqlEditorView() : base("SQL — Ctrl+Enter or F5 to run  ·  Ctrl+Up/Down history")
    {
        _text = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            WordWrap = false,
            AllowsTab = false,
            CanFocus = true
        };

        _text.KeyPress += OnKeyPress;
        Add(_text);
    }

    public string Query => _text.Text?.ToString() ?? string.Empty;

    public void SetQuery(string query)
    {
        _text.Text = query ?? string.Empty;
        _text.MoveEnd();
        _text.SetNeedsDisplay();
    }

    public void RecordSubmit(string query) => _history.Push(query);

    public void Focus() => _text.SetFocus();

    private void OnKeyPress(View.KeyEventEventArgs e)
    {
        var key = e.KeyEvent.Key;

        if (key == (Key.CursorUp | Key.CtrlMask))
        {
            var previous = _history.Older(_text.Text?.ToString() ?? string.Empty);
            if (previous is not null) SetQuery(previous);
            e.Handled = true;
        }
        else if (key == (Key.CursorDown | Key.CtrlMask))
        {
            var next = _history.Newer();
            if (next is not null) SetQuery(next);
            e.Handled = true;
        }
    }
}
