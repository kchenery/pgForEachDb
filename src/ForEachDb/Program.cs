using System.Diagnostics;
using CommandLine;
using Dapper;
using ForEachDb;
using ForEachDbQueries;
using Npgsql;

string connectionString = "";
string query = "";
List<string> ignoreDatabases = new();

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
        if (options.IncludePostgresDb) dbFinder.IgnorePostgresDb();
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

if (connectionString != "")
{
    var connection = new NpgsqlConnection(connectionString);
    
    Console.WriteLine("Finding databases...");
    var dbFinder = new DatabaseFinder()
        .IgnorePostgresDb()
        .IgnoreTemplateDb()
        .Query();

    var databases = (await connection.QueryAsync<string>(dbFinder.RawSql, dbFinder.Parameters)).ToList().Order();

    var forEachDb = new ForEachDbRunner(connectionString, null);
    await forEachDb.RunQueryAsync(databases, query);
}