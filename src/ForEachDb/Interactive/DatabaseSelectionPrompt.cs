using Spectre.Console;

namespace ForEachDb.Interactive;

public static class DatabaseSelectionPrompt
{
    public static IReadOnlyList<string> Ask(
        IReadOnlyList<string> available,
        IReadOnlyList<string> preselected)
    {
        var prompt = new MultiSelectionPrompt<string>()
            .Title("[bold]Select databases to target:[/]")
            .PageSize(20)
            .MoreChoicesText("[dim](↑/↓ to scroll)[/]")
            .InstructionsText("[dim]([blue]<space>[/] to toggle · [green]<enter>[/] to confirm)[/]")
            .AddChoices(available);

        var preset = preselected.Count > 0 ? preselected : available;
        foreach (var db in preset.Where(available.Contains))
            prompt.Select(db);

        return AnsiConsole.Prompt(prompt);
    }
}
