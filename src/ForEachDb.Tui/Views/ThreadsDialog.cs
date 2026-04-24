using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public static class ThreadsDialog
{
    public static int? Prompt(int current)
    {
        var field = new TextField(current.ToString())
        {
            X = 14,
            Y = 1,
            Width = 10
        };

        var error = new Label(string.Empty)
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill() - 2,
            ColorScheme = Colors.Error
        };

        int? result = null;
        var ok = new Button("OK", is_default: true);
        var cancel = new Button("Cancel");

        var dialog = new Dialog("Threads", 40, 9, ok, cancel);
        dialog.Add(new Label("Threads (1-64):") { X = 1, Y = 1 }, field, error);

        ok.Clicked += () =>
        {
            if (!int.TryParse(field.Text.ToString(), out var value) || value is < 1 or > 64)
            {
                error.Text = "Must be a number between 1 and 64.";
                error.SetNeedsDisplay();
                return;
            }

            result = value;
            Application.RequestStop();
        };

        cancel.Clicked += () => Application.RequestStop();

        Application.Run(dialog);
        return result;
    }
}
