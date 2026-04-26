using Dapper;
using ForEachDbQueries;
using Npgsql;

namespace ForEachDb.Desktop.Services;

public static class DatabaseProbe
{
    private const string DiscoveryQuery = @"
        SELECT datname AS ""Name"", datistemplate AS ""IsTemplate""
        FROM pg_database
        WHERE datallowconn = true AND datname != 'rdsadmin'
        ORDER BY datname";

    public static async Task<IReadOnlyList<DiscoveredDatabase>> DiscoverAsync(
        ConnectionSettings settings,
        CancellationToken ct = default)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            Username = settings.Username,
            Password = settings.Password
        };

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<DiscoveredDatabase>(DiscoveryQuery);
        return rows.ToList();
    }
}
