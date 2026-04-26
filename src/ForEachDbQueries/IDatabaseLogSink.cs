namespace ForEachDbQueries;

public interface IDatabaseLogSink
{
    void Append(DatabaseLogEntry entry);
}
