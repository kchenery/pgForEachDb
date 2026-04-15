using System.Collections.Concurrent;
using System.Text;
using Dapper;
using Npgsql;

namespace ForEachDbQueries;

public class ForEachDbRunner : IForEachDbRunner
{
    private readonly string _connectionString;

    public ForEachDbRunner(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection SwitchDatabaseConnection(string database)
    {
        var csBuilder = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Database = database,
        };

        return new NpgsqlConnection(csBuilder.ConnectionString);
    }

    public async Task<IEnumerable<TQueryResult>> RunQueryAsync<TQueryResult>(
        IEnumerable<string> databases,
        string queryTemplate,
        int numberOfThreads = -1,
        IProgress<DatabaseStatus>? progress = null)
    {
        numberOfThreads = numberOfThreads < -1 ? -1 : numberOfThreads;

        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = numberOfThreads
        };

        var allResults = new ConcurrentBag<TQueryResult>();

        await Parallel.ForEachAsync(databases, options, async (database, token) =>
        {
            try
            {
                progress?.Report(new DatabaseStatus(database, DatabaseRunState.Running));

                await using var connection = SwitchDatabaseConnection(database);
                await connection.OpenAsync(token);
                var results = (await connection.QueryAsync<TQueryResult>(queryTemplate)).Where(r => r != null).ToArray();

                foreach (var result in results)
                {
                    allResults.Add(result);
                }

                progress?.Report(new DatabaseStatus(database, DatabaseRunState.Succeeded));
            }
            catch (Exception ex)
            {
                progress?.Report(new DatabaseStatus(database, DatabaseRunState.Failed, ex.Message));
            }
        });

        return allResults;
    }

    public async Task RunQueryAsync(
        IEnumerable<string> databases,
        string queryTemplate,
        int numberOfThreads = -1,
        IProgress<DatabaseStatus>? progress = null)
    {
        await RunQueryAsync<object>(databases, TidyQuery(queryTemplate), numberOfThreads, progress);
    }

    public string TidyQuery(string queryTemplate)
    {
        var query = new StringBuilder();
        query.Append(queryTemplate.Trim());

        if(query[^1] != ';')
        {
            query.Append(';');
        }

        return query.ToString();
    }
}
