using AwesomeAssertions;
using NUnit.Framework;

namespace ForEachDbQueries.Tests;

public class RunSessionTests
{
    private static readonly ConnectionSettings Settings = new(
        Host: "localhost",
        Port: 5432,
        Database: "postgres",
        Username: "admin",
        Password: "pw",
        IncludePostgresDb: false,
        IncludeTemplateDb: false,
        IgnoreDatabases: Array.Empty<string>());

    [Test]
    public void Construction_SetsInitialStateToIdle()
    {
        var session = new RunSession(Settings, new[] { "a", "b" }, new FakeRunner());

        session.IsRunning.Should().BeFalse();
        session.Statuses.Should().BeEmpty();
        session.LastResults.Should().BeEmpty();
        session.RunStartedAt.Should().BeNull();
        session.Threads.Should().Be(4);
    }

    [Test]
    public async Task RunAsync_WithEmptyQuery_ReturnsEmptyQuery_AndDoesNotFireEvents()
    {
        var session = new RunSession(Settings, new[] { "a" }, new FakeRunner());
        var started = 0;
        session.RunStarted += () => started++;

        var outcome = await session.RunAsync("   ", new[] { "a" });

        outcome.Should().Be(RunOutcome.EmptyQuery);
        started.Should().Be(0);
        session.IsRunning.Should().BeFalse();
    }

    [Test]
    public async Task RunAsync_WithNoSelection_ReturnsNothingSelected()
    {
        var session = new RunSession(Settings, new[] { "a" }, new FakeRunner());

        var outcome = await session.RunAsync("SELECT 1;", Array.Empty<string>());

        outcome.Should().Be(RunOutcome.NothingSelected);
    }

    [Test]
    public async Task RunAsync_OnSuccess_PopulatesResultsAndFiresLifecycleEvents()
    {
        var runner = new FakeRunner
        {
            ResultRows = new[]
            {
                new DatabaseRow("a", new Dictionary<string, object?> { ["x"] = 1 })
            }
        };
        var session = new RunSession(Settings, new[] { "a" }, runner);

        var started = 0;
        RunOutcome? completed = null;
        session.RunStarted += () => started++;
        session.RunCompleted += o => completed = o;

        runner.FinishWith(result: true);
        var outcome = await session.RunAsync("SELECT 1;", new[] { "a" });

        outcome.Kind.Should().Be(RunOutcomeKind.Completed);
        session.LastResults.Should().HaveCount(1);
        started.Should().Be(1);
        completed!.Kind.Should().Be(RunOutcomeKind.Completed);
        session.IsRunning.Should().BeFalse();
        session.RunStartedAt.Should().NotBeNull();
    }

    [Test]
    public async Task RunAsync_WhenRunnerThrows_FiresFailedOutcomeWithMessage()
    {
        var runner = new FakeRunner { Exception = new InvalidOperationException("kaboom") };
        var session = new RunSession(Settings, new[] { "a" }, runner);

        runner.FinishWith(result: true);
        var outcome = await session.RunAsync("SELECT 1;", new[] { "a" });

        outcome.Kind.Should().Be(RunOutcomeKind.Failed);
        outcome.Message.Should().Be("kaboom");
        session.IsRunning.Should().BeFalse();
    }

    [Test]
    public async Task RunAsync_WhenRunnerCancels_FiresCancelledOutcome()
    {
        var runner = new FakeRunner { CancelOnRun = true };
        var session = new RunSession(Settings, new[] { "a" }, runner);

        runner.FinishWith(result: true);
        var outcome = await session.RunAsync("SELECT 1;", new[] { "a" });

        outcome.Kind.Should().Be(RunOutcomeKind.Cancelled);
    }

    [Test]
    public async Task RunAsync_RelaysProgressAndLogEvents()
    {
        var runner = new FakeRunner
        {
            DuringRun = (progress, sink, _) =>
            {
                progress?.Report(new DatabaseStatus("a", DatabaseRunState.Running));
                sink?.Append(new DatabaseLogEntry("a", DateTimeOffset.UtcNow, DatabaseLogLevel.Info, "hello"));
            }
        };
        var session = new RunSession(Settings, new[] { "a" }, runner);

        DatabaseStatus? lastStatus = null;
        DatabaseLogEntry? lastEntry = null;
        session.StatusChanged += s => lastStatus = s;
        session.LogEntryAppended += e => lastEntry = e;

        runner.FinishWith(result: true);
        await session.RunAsync("SELECT 1;", new[] { "a" });

        lastStatus!.DatabaseName.Should().Be("a");
        lastStatus.State.Should().Be(DatabaseRunState.Running);
        lastEntry!.Message.Should().Be("hello");
        session.Statuses["a"].State.Should().Be(DatabaseRunState.Running);
    }

    [Test]
    public async Task RunAsync_WhileAlreadyRunning_ReturnsAlreadyRunning()
    {
        var runner = new FakeRunner();
        var session = new RunSession(Settings, new[] { "a" }, runner);

        var first = session.RunAsync("SELECT 1;", new[] { "a" });

        // Second call while first is in-flight must short-circuit.
        var second = await session.RunAsync("SELECT 2;", new[] { "a" });
        second.Should().Be(RunOutcome.AlreadyRunning);

        runner.FinishWith(result: true);
        await first;
    }

    private sealed class FakeRunner : IForEachDbRunner
    {
        public IReadOnlyList<DatabaseRow> ResultRows { get; set; } = Array.Empty<DatabaseRow>();
        public Exception? Exception { get; set; }
        public bool CancelOnRun { get; set; }
        public Action<IProgress<DatabaseStatus>?, IDatabaseLogSink?, CancellationToken>? DuringRun { get; set; }

        private readonly TaskCompletionSource<bool> _finish = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void FinishWith(bool result) => _finish.TrySetResult(result);

        public async Task<IReadOnlyList<DatabaseRow>> RunQueryAsDynamicAsync(
            IEnumerable<string> databases,
            string queryTemplate,
            int numberOfThreads = -1,
            IProgress<DatabaseStatus>? progress = null,
            IDatabaseLogSink? logSink = null,
            CancellationToken cancellationToken = default)
        {
            DuringRun?.Invoke(progress, logSink, cancellationToken);
            if (Exception is not null) throw Exception;
            if (CancelOnRun) throw new OperationCanceledException();

            await _finish.Task;
            return ResultRows;
        }

        public Task<IReadOnlyList<TQueryResult>> RunQueryAsync<TQueryResult>(
            IEnumerable<string> databases, string queryTemplate, int numberOfThreads = -1,
            IProgress<DatabaseStatus>? progress = null, IDatabaseLogSink? logSink = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException("RunSession only uses RunQueryAsDynamicAsync");

        public Task RunQueryAsync(
            IEnumerable<string> databases, string queryTemplate, int numberOfThreads = -1,
            IProgress<DatabaseStatus>? progress = null, IDatabaseLogSink? logSink = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException("RunSession only uses RunQueryAsDynamicAsync");
    }
}
