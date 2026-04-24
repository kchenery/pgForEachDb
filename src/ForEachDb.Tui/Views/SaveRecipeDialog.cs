using ForEachDb.Tui.Infrastructure;
using ForEachDb.Tui.Models;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public static class SaveRecipeDialog
{
    public static bool Prompt(RecipeStore store, Recipe template, out string? savedName)
    {
        var nameField = new TextField(template.Name)
        {
            X = 10,
            Y = 1,
            Width = 40
        };

        var error = new Label(string.Empty)
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill() - 2,
            ColorScheme = Colors.Error
        };

        var ok = new Button("Save", is_default: true);
        var cancel = new Button("Cancel");

        var dialog = new Dialog("Save recipe", 60, 9, ok, cancel);
        dialog.Add(new Label("Name:") { X = 1, Y = 1 }, nameField, error);

        string? result = null;

        ok.Clicked += () =>
        {
            var name = nameField.Text?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                error.Text = "Name is required.";
                error.SetNeedsDisplay();
                return;
            }

            if (store.Exists(name) && !string.Equals(name, template.Name, StringComparison.OrdinalIgnoreCase))
            {
                var overwrite = MessageBox.Query(
                    "Overwrite?",
                    $"A recipe named \"{name}\" already exists. Overwrite?",
                    "Overwrite", "Cancel");
                if (overwrite != 0) return;
            }

            result = name;
            Application.RequestStop();
        };

        cancel.Clicked += () => Application.RequestStop();

        Application.Run(dialog);

        if (result is null)
        {
            savedName = null;
            return false;
        }

        var recipe = template with { Name = result };
        store.Save(recipe);
        savedName = result;
        return true;
    }
}
