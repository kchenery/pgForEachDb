namespace ForEachDbQueries;

public enum RunOutcomeKind
{
    Completed,
    Cancelled,
    AlreadyRunning,
    EmptyQuery,
    NothingSelected,
    Failed
}

public sealed record RunOutcome(RunOutcomeKind Kind, string? Message = null)
{
    public static RunOutcome Completed { get; } = new(RunOutcomeKind.Completed);
    public static RunOutcome Cancelled { get; } = new(RunOutcomeKind.Cancelled);
    public static RunOutcome AlreadyRunning { get; } = new(RunOutcomeKind.AlreadyRunning);
    public static RunOutcome EmptyQuery { get; } = new(RunOutcomeKind.EmptyQuery);
    public static RunOutcome NothingSelected { get; } = new(RunOutcomeKind.NothingSelected);
    public static RunOutcome Failed(string message) => new(RunOutcomeKind.Failed, message);
}
