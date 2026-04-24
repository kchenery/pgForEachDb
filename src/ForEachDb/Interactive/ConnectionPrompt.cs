using Dapper;
using ForEachDbQueries;
using ForEachDbQueries.DapperExtensions;
using Npgsql;
using Spectre.Console;

namespace ForEachDb.Interactive;

public sealed record ConnectionResult(
    ConnectionSettings Settings,
    IReadOnlyList<string> Databases,
    Recipe? Recipe);

public static class ConnectionPrompt
{
    public static async Task<ConnectionResult?> AskAsync(RecipeStore recipes)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Connect to PostgreSQL[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var usingRecipe = false;
        Recipe? chosen = null;

        if (recipes.Load().Count > 0)
        {
            var start = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Start from a saved recipe or enter connection details fresh?")
                .AddChoices("Enter connection details", "Load recipe"));

            if (start == "Load recipe")
            {
                chosen = RecipePrompts.Pick(recipes);
                usingRecipe = chosen is not null;
            }
        }

        var host     = Input("Host",     chosen?.Connection.Host     ?? "localhost");
        var port     = Input("Port",     (chosen?.Connection.Port    ?? 5432).ToString(), int.TryParse);
        var database = Input("Database", chosen?.Connection.Database ?? "postgres");
        var username = Input("Username", chosen?.Connection.Username ?? "postgres");
        var password = AnsiConsole.Prompt(new TextPrompt<string>("[bold]Password:[/]").Secret().AllowEmpty());

        var includePostgres = chosen?.Connection.IncludePostgresDb ?? AnsiConsole.Confirm("Include the [blue]postgres[/] database?", false);
        var includeTemplate = chosen?.Connection.IncludeTemplateDb ?? AnsiConsole.Confirm("Include [blue]template[/] databases?", false);

        var ignoreInput = chosen is not null
            ? string.Join(", ", chosen.Connection.IgnoreDatabases)
            : AnsiConsole.Prompt(new TextPrompt<string>("Ignore (comma-sep names, or empty):").AllowEmpty().DefaultValue(""));

        var ignore = (ignoreInput ?? string.Empty)
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var settings = new ConnectionSettings(
            host, int.Parse(port), database, username, password,
            includePostgres, includeTemplate, ignore);

        AnsiConsole.WriteLine();

        IReadOnlyList<string>? databases = null;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Connecting to [cyan]{host}:{port}[/]…", async _ =>
            {
                try
                {
                    databases = await ProbeAsync(settings);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] {ex.InnerException?.Message ?? ex.Message}");
                }
            });

        if (databases is null) return null;
        if (databases.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Connected but no databases matched the current filters.[/]");
            return null;
        }

        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Connected — found [bold]{databases.Count}[/] database(s).");
        return new ConnectionResult(settings, databases, usingRecipe ? chosen : null);
    }

    private static string Input(string label, string defaultValue) =>
        AnsiConsole.Prompt(new TextPrompt<string>($"[bold]{label}:[/]")
            .DefaultValue(defaultValue)
            .ShowDefaultValue()
            .AllowEmpty());

    private static string Input(string label, string defaultValue, TryParse<int> tryParse)
    {
        while (true)
        {
            var raw = Input(label, defaultValue);
            if (tryParse(raw, out var _))
                return raw;
            AnsiConsole.MarkupLine("[red]Port must be a whole number.[/]");
        }
    }

    private delegate bool TryParse<T>(string input, out T value);

    private static async Task<IReadOnlyList<string>> ProbeAsync(ConnectionSettings settings)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            Username = settings.Username,
            Password = settings.Password
        };

        var finder = new DatabaseFinder();
        if (!settings.IncludePostgresDb) finder.IgnorePostgresDb();
        if (!settings.IncludeTemplateDb) finder.IgnoreTemplateDb();
        if (settings.IgnoreDatabases.Count > 0) finder.IgnoreDatabases(settings.IgnoreDatabases);
        finder.OrderByName();

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return (await connection.QueryAsync<string>(finder)).ToList();
    }
}
