using CommandLine;
using ForEachDb;
using ForEachDbQueries;
using ForEachDbQueries.DapperExtensions;
using Npgsql;
using Spectre.Console;

string connectionString = "";
string query = "";
List<string> ignoreDatabases = new();

var dbFinder = new DatabaseFinder();
var threads = -1;

// Parse arguments
Parser.Default.ParseArguments<Options>(args)
    .WithParsed(options =>
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options),"Must specify options");
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

// Find databases and run query against them
if (!string.IsNullOrEmpty(connectionString))
{
    await using var connection = new NpgsqlConnection(connectionString);

    var databases = (await connection.QueryAsync<string>(dbFinder)).Order().ToList();

    if (databases.Count != 0)
    {
        var forEachDb = new ForEachDbRunner(connectionString);

        await AnsiConsole.Live(Text.Empty).StartAsync(async ctx =>
        {
            using var renderer = new LiveProgressRenderer(ctx, databases.Count);
            await forEachDb.RunQueryAsync(databases, query, threads, renderer);
            ctx.UpdateTarget(renderer.BuildDisplay());
        });
    }
    else
    {
        AnsiConsole.MarkupLine("[yellow]No databases found to run query against[/]");
    }
}
