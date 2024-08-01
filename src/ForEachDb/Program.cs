using CommandLine;
using ForEachDb;
using ForEachDbQueries;
using ForEachDbQueries.DapperExtensions;
using Microsoft.Extensions.Logging;
using Npgsql;

const string loggingTimeFormat = "HH:mm:ss | ";
string connectionString = "";
string query = "";
List<string> ignoreDatabases = new();

using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
    .SetMinimumLevel(LogLevel.Trace)
    .AddSimpleConsole(opt =>
    {
        opt.IncludeScopes = false;
        opt.TimestampFormat = loggingTimeFormat;
    })
);

var logger = loggerFactory.CreateLogger<ForEachDbRunner>();

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
        
        Console.WriteLine();
        Console.WriteLine($"Host:     {csBuilder.Host}");
        Console.WriteLine($"Username: {csBuilder.Username}");
        Console.WriteLine($"Threads:  {threads}");
    });

// Disable query timeout
Dapper.SqlMapper.Settings.CommandTimeout = 0;

// Find databases and run query against them
if (!string.IsNullOrEmpty(connectionString))
{
    var connection = new NpgsqlConnection(connectionString);
    
    var databases = (await connection.QueryAsync<string>(dbFinder)).Order().ToList();

    if (databases.Count != 0)
    {
        var forEachDb = new ForEachDbRunner(connectionString, logger);
        await forEachDb.RunQueryAsync(databases, query, threads);
    }
    else
    {
        logger.LogWarning("No databases found to run query against");
    }
}