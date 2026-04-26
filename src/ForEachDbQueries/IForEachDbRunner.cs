namespace ForEachDbQueries;

public interface IForEachDbRunner
{
    Task<IReadOnlyList<TQueryResult>> RunQueryAsync<TQueryResult>(
        IEnumerable<string> databases,
        string queryTemplate,
        int numberOfThreads = -1,
        IProgress<DatabaseStatus>? progress = null,
        IDatabaseLogSink? logSink = null,
        CancellationToken cancellationToken = default);

    Task RunQueryAsync(
        IEnumerable<string> databases,
        string queryTemplate,
        int numberOfThreads = -1,
        IProgress<DatabaseStatus>? progress = null,
        IDatabaseLogSink? logSink = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatabaseRow>> RunQueryAsDynamicAsync(
        IEnumerable<string> databases,
        string queryTemplate,
        int numberOfThreads = -1,
        IProgress<DatabaseStatus>? progress = null,
        IDatabaseLogSink? logSink = null,
        CancellationToken cancellationToken = default);
}
