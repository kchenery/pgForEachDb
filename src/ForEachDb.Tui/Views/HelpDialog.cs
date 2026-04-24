using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public static class HelpDialog
{
    private static readonly string[] Lines =
    [
        "Run",
        "  F5 / Ctrl+Enter       Run query across selected databases",
        "  F6                    Cancel active run",
        "  Ctrl+T                Change thread count (1–64)",
        "",
        "Selection",
        "  Space                 Toggle the highlighted database",
        "  Ctrl+A                Select all databases",
        "  Ctrl+N                Select none",
        "",
        "SQL editor",
        "  Ctrl+Up / Ctrl+Down   Previous / next query from session history",
        "",
        "Log + results",
        "  Ctrl+L                Cycle log filter (all / selected db / failed only)",
        "  Ctrl+R                Open the results grid from the last run",
        "  Ctrl+E                Export results as CSV (inside the grid)",
        "  Enter on failed row   Show the full error message",
        "",
        "Recipes",
        "  Ctrl+S                Save the current connection + selection + query",
        "  (Load recipes from the connection dialog at launch)",
        "",
        "App",
        "  F1                    This help",
        "  Esc / Ctrl+Q          Quit (Esc during a run cancels first)"
    ];

    public static void Show()
    {
        var close = new Button("Close", is_default: true);
        var dialog = new Dialog("Keybindings", 70, Math.Min(28, Lines.Length + 6), close);

        var text = new TextView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 2,
            ReadOnly = true,
            WordWrap = false,
            Text = string.Join(Environment.NewLine, Lines)
        };

        dialog.Add(text);
        close.Clicked += () => Application.RequestStop();
        Application.Run(dialog);
    }
}
