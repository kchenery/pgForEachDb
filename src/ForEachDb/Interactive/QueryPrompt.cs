using Spectre.Console;

namespace ForEachDb.Interactive;

public static class QueryPrompt
{
    /// <summary>
    /// Returns a query or null if the user wants to abort. Terminator: a line containing only ";;"
    /// (or a trailing ";" on a single-line input).
    /// </summary>
    public static string? Ask(string? seed = null)
    {
        AnsiConsole.MarkupLine("[dim]Enter SQL. Finish with a line of [bold];;[/] (or single line ending in [bold];[/]). Empty line on first prompt returns to menu.[/]");

        if (!string.IsNullOrWhiteSpace(seed))
            AnsiConsole.MarkupLineInterpolated($"[dim]Last query:[/] {seed.ReplaceLineEndings(" ⏎ ")}");

        var buffer = new List<string>();
        while (true)
        {
            var prefix = buffer.Count == 0 ? "[bold green]SQL>[/] " : "[dim]...>[/] ";
            AnsiConsole.Markup(prefix);
            var line = Console.ReadLine();

            if (line is null)
                return null;

            if (buffer.Count == 0 && line.Trim().Length == 0)
                return null;

            if (line.Trim() == ";;")
                break;

            buffer.Add(line);

            // Single-line shortcut: if the first non-empty line ends in ';', submit.
            if (buffer.Count == 1 && buffer[0].TrimEnd().EndsWith(';'))
                break;
        }

        return string.Join(Environment.NewLine, buffer).Trim();
    }
}
