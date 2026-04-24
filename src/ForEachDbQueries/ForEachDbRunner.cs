using System.Collections.Concurrent;
using System.Diagnostics;
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

    public async Task<IReadOnlyList<TQueryResult>> RunQueryAsync<TQueryResult>(
        IEnumerable<string> databases,
        string queryTemplate,
        int numberOfThreads = -1,
        IProgress<DatabaseStatus>? progress = null,
        IDatabaseLogSink? logSink = null,
        CancellationToken cancellationToken = default)
    {
        var databaseList = databases.ToList();
        var query = TidyQuery(queryTemplate);

        SeedPending(databaseList, progress);

        var options = NormaliseParallelOptions(numberOfThreads, cancellationToken);
        var allResults = new ConcurrentBag<TQueryResult>();

        await Parallel.ForEachAsync(databaseList, options, async (database, token) =>
        {
            await ExecutePerDatabase(database, progress, logSink, token, async (connection, ct) =>
            {
                var results = (await connection.QueryAsync<TQueryResult>(new CommandDefinition(query, cancellationToken: ct)))
                    .Where(r => r is not null)
                    .ToArray();

                foreach (var result in results)
                {
                    allResults.Add(result);
                }

                return results.Length;
            });
        });

        return allResults.ToList();
    }

    public Task RunQueryAsync(
        IEnumerable<string> databases,
        string queryTemplate,
        int numberOfThreads = -1,
        IProgress<DatabaseStatus>? progress = null,
        IDatabaseLogSink? logSink = null,
        CancellationToken cancellationToken = default) =>
        RunQueryAsync<object>(databases, queryTemplate, numberOfThreads, progress, logSink, cancellationToken);

    public async Task<IReadOnlyList<DatabaseRow>> RunQueryAsDynamicAsync(
        IEnumerable<string> databases,
        string queryTemplate,
        int numberOfThreads = -1,
        IProgress<DatabaseStatus>? progress = null,
        IDatabaseLogSink? logSink = null,
        CancellationToken cancellationToken = default)
    {
        var databaseList = databases.ToList();
        var query = TidyQuery(queryTemplate);

        SeedPending(databaseList, progress);

        var options = NormaliseParallelOptions(numberOfThreads, cancellationToken);
        var allRows = new ConcurrentBag<DatabaseRow>();

        await Parallel.ForEachAsync(databaseList, options, async (database, token) =>
        {
            await ExecutePerDatabase(database, progress, logSink, token, async (connection, ct) =>
            {
                var rows = await connection.QueryAsync(new CommandDefinition(query, cancellationToken: ct));
                var count = 0;

                foreach (var row in rows)
                {
                    if (row is IDictionary<string, object> values)
                    {
                        allRows.Add(new DatabaseRow(database, new Dictionary<string, object?>(
                            values.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)))));
                        count++;
                    }
                }

                return count;
            });
        });

        return allRows.ToList();
    }

    public string TidyQuery(string queryTemplate)
    {
        var query = new StringBuilder();
        query.Append(queryTemplate.Trim());

        if (query.Length == 0 || query[^1] != ';')
        {
            query.Append(';');
        }

        return query.ToString();
    }

    private static void SeedPending(IEnumerable<string> databases, IProgress<DatabaseStatus>? progress)
    {
        if (progress is null) return;

        foreach (var database in databases)
        {
            progress.Report(new DatabaseStatus(database, DatabaseRunState.Pending));
        }
    }

    private static ParallelOptions NormaliseParallelOptions(int numberOfThreads, CancellationToken cancellationToken) =>
        new()
        {
            MaxDegreeOfParallelism = numberOfThreads < -1 ? -1 : numberOfThreads,
            CancellationToken = cancellationToken
        };

    private async Task ExecutePerDatabase(
        string database,
        IProgress<DatabaseStatus>? progress,
        IDatabaseLogSink? logSink,
        CancellationToken token,
        Func<NpgsqlConnection, CancellationToken, Task<int>> execute)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            progress?.Report(new DatabaseStatus(database, DatabaseRunState.Running));
            logSink?.Append(new DatabaseLogEntry(database, DateTimeOffset.UtcNow, DatabaseLogLevel.Info, "Query started"));

            await using var connection = SwitchDatabaseConnection(database);
            connection.Notice += (_, args) =>
                logSink?.Append(new DatabaseLogEntry(
                    database,
                    DateTimeOffset.UtcNow,
                    MapNoticeSeverity(args.Notice.Severity),
                    args.Notice.MessageText));

            await connection.OpenAsync(token);
            var rowCount = await execute(connection, token);

            var duration = Stopwatch.GetElapsedTime(started);
            progress?.Report(new DatabaseStatus(database, DatabaseRunState.Succeeded, Duration: duration, RowCount: rowCount));
            logSink?.Append(new DatabaseLogEntry(
                database,
                DateTimeOffset.UtcNow,
                DatabaseLogLevel.Info,
                $"Completed in {duration.TotalMilliseconds:F0} ms ({rowCount} rows)"));
        }
        catch (OperationCanceledException)
        {
            var duration = Stopwatch.GetElapsedTime(started);
            progress?.Report(new DatabaseStatus(database, DatabaseRunState.Cancelled, "Cancelled", Duration: duration));
            logSink?.Append(new DatabaseLogEntry(database, DateTimeOffset.UtcNow, DatabaseLogLevel.Warning, "Cancelled"));
        }
        catch (Exception ex)
        {
            var duration = Stopwatch.GetElapsedTime(started);
            progress?.Report(new DatabaseStatus(database, DatabaseRunState.Failed, ex.Message, Duration: duration));
            logSink?.Append(new DatabaseLogEntry(database, DateTimeOffset.UtcNow, DatabaseLogLevel.Error, ex.Message));
        }
    }

    private static DatabaseLogLevel MapNoticeSeverity(string? severity) => severity switch
    {
        "WARNING" => DatabaseLogLevel.Warning,
        "ERROR" or "FATAL" or "PANIC" => DatabaseLogLevel.Error,
        _ => DatabaseLogLevel.Notice
    };
}
