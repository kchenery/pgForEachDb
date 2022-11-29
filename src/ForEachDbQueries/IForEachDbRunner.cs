namespace ForEachDbQueries;

public interface IForEachDbRunner
{
    public Task<IEnumerable<TQueryResult>> RunQueryAsync<TQueryResult>(IEnumerable<string> databases, string queryTemplate, int numberOfThreads);
}