namespace ForEachDb.Desktop.ViewModels;

public static class WorkspaceDefaults
{
    public const string AllDatabasesFilter = "(all databases)";

    public const int MinThreads = 1;
    public const int MaxThreads = 64;
    public const int DefaultThreads = 4;

    // NumericUpDown binds decimal, so expose the range as decimals for XAML.
    public const decimal MinThreadsDecimal = MinThreads;
    public const decimal MaxThreadsDecimal = MaxThreads;

    public const string CsvFileNamePattern = "pgForEachDb-{0:yyyyMMdd-HHmmss}.csv";
    public const string LogFileNamePattern = "pgForEachDb-{0:yyyyMMdd-HHmmss}.log";
}
