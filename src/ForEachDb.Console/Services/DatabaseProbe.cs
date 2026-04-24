using Dapper;
using ForEachDbQueries;
using ForEachDbQueries.DapperExtensions;
using Npgsql;

namespace ForEachDb.Console.Services;

public static class DatabaseProbe
{
    public static async Task<IReadOnlyList<string>> DiscoverAsync(ConnectionSettings settings, CancellationToken ct = default)
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
        await connection.OpenAsync(ct);
        return (await connection.QueryAsync<string>(finder)).ToList();
    }
}
