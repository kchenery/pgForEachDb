using ForEachDb.Tui.Infrastructure;
using ForEachDb.Tui.Models;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public static class LoadRecipeDialog
{
    public static Recipe? Prompt(RecipeStore store)
    {
        var recipes = store.Load().ToList();

        if (recipes.Count == 0)
        {
            MessageBox.Query("Recipes", $"No recipes saved yet.\nStore location: {store.Path}", "OK");
            return null;
        }

        Recipe? result = null;

        var list = new ListView(recipes.Select(FormatRow).ToList())
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 3,
            AllowsMarking = false,
            CanFocus = true
        };

        var hint = new Label("Enter to load  ·  Del to delete  ·  Esc to cancel")
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill() - 2,
            ColorScheme = Colors.Dialog
        };

        var load = new Button("Load", is_default: true);
        var cancel = new Button("Cancel");

        var dialog = new Dialog("Load recipe", 70, 18, load, cancel);
        dialog.Add(list, hint);

        load.Clicked += () =>
        {
            if (list.SelectedItem >= 0 && list.SelectedItem < recipes.Count)
            {
                result = recipes[list.SelectedItem];
                Application.RequestStop();
            }
        };

        cancel.Clicked += () => Application.RequestStop();

        list.OpenSelectedItem += _ => load.OnClicked();

        list.KeyPress += e =>
        {
            if (e.KeyEvent.Key == Key.DeleteChar || e.KeyEvent.Key == Key.Backspace)
            {
                var idx = list.SelectedItem;
                if (idx < 0 || idx >= recipes.Count) return;

                var target = recipes[idx];
                var confirm = MessageBox.Query(
                    "Delete recipe",
                    $"Delete \"{target.Name}\"? This cannot be undone.",
                    "Delete", "Cancel");
                if (confirm != 0) return;

                store.Delete(target.Name);
                recipes.RemoveAt(idx);

                if (recipes.Count == 0)
                {
                    Application.RequestStop();
                    return;
                }

                list.SetSource(recipes.Select(FormatRow).ToList());
                list.SelectedItem = Math.Min(idx, recipes.Count - 1);
                list.SetNeedsDisplay();
                e.Handled = true;
            }
        };

        Application.Run(dialog);
        return result;
    }

    private static string FormatRow(Recipe recipe) =>
        $"{recipe.Name}  —  {recipe.Connection.Username}@{recipe.Connection.Host}:{recipe.Connection.Port}  ·  {recipe.SelectedDatabases.Count} db  ·  {Preview(recipe.Query)}";

    private static string Preview(string query)
    {
        var single = query.ReplaceLineEndings(" ").Trim();
        return single.Length <= 40 ? single : single[..37] + "…";
    }
}
