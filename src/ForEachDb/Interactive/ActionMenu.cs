using Spectre.Console;

namespace ForEachDb.Interactive;

public enum NextAction
{
    RunSameQuery,
    NewQuery,
    ChangeSelection,
    ChangeThreads,
    SaveRecipe,
    ViewResults,
    ExportCsv,
    Reconnect,
    Quit
}

public static class ActionMenu
{
    public static NextAction Ask(bool hasResults, bool hasQuery)
    {
        var choices = new List<(string Label, NextAction Action)>();
        if (hasQuery) choices.Add(("Run same query again",         NextAction.RunSameQuery));
        choices.Add(("Enter a new query",                          NextAction.NewQuery));
        choices.Add(("Change database selection",                  NextAction.ChangeSelection));
        choices.Add(("Change thread count",                        NextAction.ChangeThreads));
        choices.Add(("Save current as recipe",                     NextAction.SaveRecipe));
        if (hasResults) choices.Add(("View results",               NextAction.ViewResults));
        if (hasResults) choices.Add(("Export results to CSV",      NextAction.ExportCsv));
        choices.Add(("Connect to a different cluster",             NextAction.Reconnect));
        choices.Add(("Quit",                                       NextAction.Quit));

        var pick = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("\n[bold]What next?[/]")
            .PageSize(10)
            .AddChoices(choices.Select(c => c.Label)));

        return choices.First(c => c.Label == pick).Action;
    }

    public static int AskThreads(int current)
    {
        return AnsiConsole.Prompt(new TextPrompt<int>("Threads (1-64):")
            .DefaultValue(current)
            .Validate(n => n is >= 1 and <= 64
                ? ValidationResult.Success()
                : ValidationResult.Error("Must be between 1 and 64")));
    }
}
