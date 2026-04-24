using ForEachDbQueries;
using Spectre.Console;

namespace ForEachDb.Interactive;

public static class InteractiveApp
{
    public static async Task RunAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("pgForEachDb").Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[dim]Run SQL across many PostgreSQL databases in parallel.[/]");
        AnsiConsole.WriteLine();

        var recipes = new RecipeStore();

        while (await RunSessionLoopAsync(recipes))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[dim]reconnect[/]"));
            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine("[cyan]Goodbye.[/]");
    }

    /// <summary>
    /// Runs one connection's full interaction loop. Returns true to reconnect to a different cluster,
    /// false to exit the app.
    /// </summary>
    private static async Task<bool> RunSessionLoopAsync(RecipeStore recipes)
    {
        var connection = await ConnectionPrompt.AskAsync(recipes);
        if (connection is null) return false;

        var session = new RunSession(connection.Settings, connection.Databases);
        var selected = (connection.Recipe?.SelectedDatabases ?? connection.Databases).ToList();
        var query = connection.Recipe?.Query ?? string.Empty;
        session.Threads = connection.Recipe?.Threads ?? 4;

        // First query — if we came from a recipe, prompt to edit or accept as-is.
        var initialSelection = true;

        while (true)
        {
            if (initialSelection)
            {
                selected = DatabaseSelectionPrompt.Ask(connection.Databases, selected).ToList();
                if (selected.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No databases selected.[/]");
                }
                initialSelection = false;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                query = QueryPrompt.Ask(null) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(query))
                {
                    AnsiConsole.MarkupLine("[dim]No query entered.[/]");
                }
            }

            if (!string.IsNullOrWhiteSpace(query) && selected.Count > 0)
            {
                AnsiConsole.WriteLine();
                var renderer = new LiveRunRenderer(session, selected);
                var outcome = await renderer.RunAsync(query);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLineInterpolated($"[bold]Run outcome:[/] {outcome.Kind}{(outcome.Message is { } m ? $" — {Markup.Escape(m)}" : "")}");

                ResultsRenderer.PrintSummary(session.LastResults);
                AnsiConsole.WriteLine();
            }

            var hasResults = session.LastResults.Count > 0;
            var hasQuery = !string.IsNullOrWhiteSpace(query);

            switch (ActionMenu.Ask(hasResults, hasQuery))
            {
                case NextAction.RunSameQuery:
                    // keep query + selection; loop will run
                    break;
                case NextAction.NewQuery:
                    query = QueryPrompt.Ask(query) ?? string.Empty;
                    break;
                case NextAction.ChangeSelection:
                    initialSelection = true;
                    break;
                case NextAction.ChangeThreads:
                    session.Threads = ActionMenu.AskThreads(session.Threads);
                    break;
                case NextAction.SaveRecipe:
                    var template = BuildRecipe(connection, selected, query, session.Threads);
                    RecipePrompts.Save(recipes, template);
                    break;
                case NextAction.ViewResults:
                    await ResultsViewer.ShowAsync(session.LastResults);
                    break;
                case NextAction.ExportCsv:
                    await ResultsRenderer.ExportCsvAsync(session.LastResults);
                    break;
                case NextAction.Reconnect:
                    return true;
                case NextAction.Quit:
                    return false;
            }
        }
    }

    private static Recipe BuildRecipe(ConnectionResult connection, IReadOnlyList<string> selection, string query, int threads) =>
        new(
            Name: connection.Recipe?.Name ?? string.Empty,
            Connection: connection.Settings with { Password = string.Empty },
            SelectedDatabases: selection.ToList(),
            Query: query,
            Threads: threads);
}
