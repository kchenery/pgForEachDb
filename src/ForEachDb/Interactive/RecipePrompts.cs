using ForEachDbQueries;
using Spectre.Console;

namespace ForEachDb.Interactive;

public static class RecipePrompts
{
    public static Recipe? Pick(RecipeStore store)
    {
        var recipes = store.Load();
        if (recipes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No recipes saved yet.[/]");
            return null;
        }

        var rows = recipes
            .Select(r => (Label: Format(r), Recipe: r))
            .ToList();

        var pick = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Pick a recipe:")
            .PageSize(15)
            .AddChoices(rows.Select(r => r.Label).Append("[dim]Cancel[/]")));

        return pick == "[dim]Cancel[/]"
            ? null
            : rows.First(r => r.Label == pick).Recipe;
    }

    public static void Save(RecipeStore store, Recipe template)
    {
        var name = AnsiConsole.Prompt(new TextPrompt<string>("Recipe name:")
            .DefaultValue(template.Name.Length > 0 ? template.Name : "")
            .AllowEmpty());

        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return;
        }

        if (store.Exists(name) && !string.Equals(name, template.Name, StringComparison.OrdinalIgnoreCase))
        {
            if (!AnsiConsole.Confirm($"Recipe [cyan]{name}[/] already exists. Overwrite?", false))
                return;
        }

        store.Save(template with { Name = name });
        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Recipe [cyan]{name}[/] saved.");
    }

    public static void Delete(RecipeStore store)
    {
        var recipes = store.Load();
        if (recipes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No recipes to delete.[/]");
            return;
        }

        var names = recipes.Select(r => r.Name).Append("[dim]Cancel[/]").ToList();
        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Delete which recipe?")
            .PageSize(15)
            .AddChoices(names));

        if (choice == "[dim]Cancel[/]") return;

        if (AnsiConsole.Confirm($"Delete [red]{choice}[/]?", false))
        {
            store.Delete(choice);
            AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Deleted [red]{choice}[/].");
        }
    }

    private static string Format(Recipe r) =>
        $"{r.Name}  ·  {r.Connection.Username}@{r.Connection.Host}:{r.Connection.Port}  ·  {r.SelectedDatabases.Count} db  ·  {Preview(r.Query)}";

    private static string Preview(string q)
    {
        var single = q.ReplaceLineEndings(" ").Trim();
        return single.Length <= 40 ? single : single[..37] + "…";
    }
}
