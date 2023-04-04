using CommandLine;
using Dapper;
using ForEachDb;
using ForEachDbQueries;
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
        if (options.IgnoreDatabases is not null) ignoreDatabases.AddRange(options.IgnoreDatabases);
        if (!options.IncludePostgresDb) dbFinder.IgnorePostgresDb();
        if (!options.IncludeTemplateDb) dbFinder.IgnoreTemplateDb();
        
        foreach (var ignoreDb in ignoreDatabases)
        {
            dbFinder.IgnoreDatabase(ignoreDb);
        }

        var csBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = options.HostName,
            Database = options.Database,
            Username = options.Username,
            Password = options.Password,
            Port = options.Port
        };

        connectionString = csBuilder.ToString();
    });

// Find databases and run query against them
if (!string.IsNullOrEmpty(connectionString))
{
    var connection = new NpgsqlConnection(connectionString);
    
    var databases = (await connection.QueryAsync<string>(dbFinder.Query().RawSql, dbFinder.Query().Parameters)).Order().ToList();

    if (databases.Any())
    {
        var forEachDb = new ForEachDbRunner(connectionString, logger);
        await forEachDb.RunQueryAsync(databases, query);
    }
    else
    {
        logger.LogWarning("No databases found to run query against");
    }
}