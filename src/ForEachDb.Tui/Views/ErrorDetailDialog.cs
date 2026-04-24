using ForEachDbQueries;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public static class ErrorDetailDialog
{
    public static void Show(DatabaseStatus status)
    {
        var close = new Button("Close", is_default: true);
        var dialog = new Dialog($"Error — {status.DatabaseName}", 80, 18, close);

        var view = new TextView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 2,
            ReadOnly = true,
            WordWrap = true,
            Text = status.ErrorMessage ?? "(no message)"
        };

        dialog.Add(view);
        close.Clicked += () => Application.RequestStop();
        Application.Run(dialog);
    }
}
