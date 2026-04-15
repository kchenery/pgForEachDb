using CommandLine;
using ForEachDb;
using ForEachDbQueries;
using ForEachDbQueries.DapperExtensions;
using Npgsql;
using Spectre.Console;

string connectionString = "";
string query = "";
bool interactive = false;

var dbFinder = new DatabaseFinder();
var threads = -1;

// Parse arguments
Parser.Default.ParseArguments<Options>(args)
    .WithParsed(options =>
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options), "Must specify options");
        }

        interactive = options.Interactive;

        if (!interactive && string.IsNullOrEmpty(options.Query))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] --query is required unless --interactive is set.");
            Environment.Exit(1);
        }

        // Handle required fields
        options.Password ??= ReadLine.ReadPassword("Password: ");

        if (options.Query is not null) query = options.Query;
        if (options.IgnoreDatabases is not null) dbFinder.IgnoreDatabases(options.IgnoreDatabases);
        if (!options.IncludePostgresDb) dbFinder.IgnorePostgresDb();
        if (!options.IncludeTemplateDb) dbFinder.IgnoreTemplateDb();

        var csBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = options.HostName,
            Database = options.Database,
            Username = options.Username,
            Password = options.Password,
            Port = options.Port
        };

        connectionString = csBuilder.ToString();

        threads = options.Threads;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Host:[/]     {Markup.Escape(csBuilder.Host ?? "")}");
        AnsiConsole.MarkupLine($"[bold]Username:[/] {Markup.Escape(csBuilder.Username ?? "")}");
        AnsiConsole.MarkupLine($"[bold]Threads:[/]  {threads}");
        AnsiConsole.WriteLine();
    });

// Disable query timeout
Dapper.SqlMapper.Settings.CommandTimeout = 0;

if (string.IsNullOrEmpty(connectionString))
    return;

await using var connection = new NpgsqlConnection(connectionString);
var allDatabases = (await connection.QueryAsync<string>(dbFinder)).Order().ToList();

if (allDatabases.Count == 0)
{
    AnsiConsole.MarkupLine("[yellow]No databases found to run query against[/]");
    return;
}

var databases = allDatabases;
var forEachDb = new ForEachDbRunner(connectionString);

if (interactive)
{
    var selectDatabases = true;

    while (true)
    {
        if (selectDatabases)
        {
            var prompt = new MultiSelectionPrompt<string>()
                .Title("[bold]Select databases to target:[/]")
                .PageSize(15)
                .InstructionsText("[dim](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
                .AddChoices(allDatabases);

            foreach (var db in databases)
            {
                prompt.Select(db);
            }

            databases = AnsiConsole.Prompt(prompt);

            AnsiConsole.MarkupLine($"[dim]Selected {databases.Count} database(s)[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Enter a query, [bold]\\r[/] to reselect databases, or empty to quit.[/]");
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold]SQL>[/] ")
                .AllowEmpty()
                .PromptStyle("green"));

        if (string.IsNullOrWhiteSpace(input))
            break;

        if (input.Trim().Equals("\\r", StringComparison.OrdinalIgnoreCase))
        {
            selectDatabases = true;
            continue;
        }

        selectDatabases = false;
        query = input;
        AnsiConsole.WriteLine();

        await AnsiConsole.Live(Text.Empty).StartAsync(async ctx =>
        {
            using var renderer = new LiveProgressRenderer(ctx, databases.Count);
            await forEachDb.RunQueryAsync(databases, query, threads, renderer);
            ctx.UpdateTarget(renderer.BuildDisplay());
        });
    }
}
else
{
    await AnsiConsole.Live(Text.Empty).StartAsync(async ctx =>
    {
        using var renderer = new LiveProgressRenderer(ctx, databases.Count);
        await forEachDb.RunQueryAsync(databases, query, threads, renderer);
        ctx.UpdateTarget(renderer.BuildDisplay());
    });
}
