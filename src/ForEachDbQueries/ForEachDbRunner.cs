using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ForEachDbQueries;

public class ForEachDbRunner : IForEachDbRunner
{
    private readonly string _connectionString;
    private ILogger _logger;

    public ForEachDbRunner(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private NpgsqlConnection SwitchDatabaseConnection(string database)
    {
        var csBuilder = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Database = database,
        };

        return new NpgsqlConnection(csBuilder.ConnectionString);
    }

    public async Task<IEnumerable<TQueryResult>> RunQueryAsync<TQueryResult>(IEnumerable<string> databases, string queryTemplate, int numberOfThreads = -1)
    {
        numberOfThreads = numberOfThreads < -1 ? -1 : numberOfThreads;
        
        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = numberOfThreads
        };

        var allResults = new List<TQueryResult>();

        await Parallel.ForEachAsync(databases, options, async (database, token) =>
        {
            _logger.LogInformation("{Database} | Query starting", database);
            var connection = SwitchDatabaseConnection(database);
            await connection.OpenAsync(token);
            var results = (await connection.QueryAsync<TQueryResult>(queryTemplate)).Where(r => r != null).ToArray();

            foreach (var result in results)
            {
                _logger.LogInformation("{Database} | {Message}", database, result?.ToString());
            }
            
            _logger.LogInformation("{Database}: Query complete", database);
            
            allResults.AddRange(results);
            await connection.CloseAsync();
        });

        return allResults;
    }

    public async Task RunQueryAsync(IEnumerable<string> databases, string queryTemplate, int numberOfThreads = -1)
    {
        await RunQueryAsync<object>(databases, TidyQuery(queryTemplate), numberOfThreads);
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