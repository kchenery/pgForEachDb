namespace ForEachDbQueries;

public static class LogExporter
{
    public static async Task WriteAsync(
        Stream destination,
        IEnumerable<DatabaseLogEntry> entries,
        CancellationToken cancellationToken = default)
    {
        await using var writer = new StreamWriter(destination, leaveOpen: true);

        foreach (var entry in entries)
        {
            var line = string.Join('\t',
                entry.Timestamp.LocalDateTime.ToString("HH:mm:ss"),
                entry.DatabaseName,
                entry.Level.ToString().ToUpperInvariant(),
                entry.Message);
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }
}
