using System.Diagnostics;
using CommandLine;
using Dapper;
using ForEachDb;
using ForEachDbQueries;
using Npgsql;

string connectionString = "";
string query = "";

// Get connection
Parser.Default.ParseArguments<Options>(args)
    .WithParsed<Options>(o =>
    {
        if (o?.Query is not null) query = o.Query;

        Debug.Assert(o != null, nameof(o) + " != null");
        var csBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = o.HostName,
            Database = o.Database,
            Username = o.Username,
            Password = o.Password,
            Port = o.Port
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