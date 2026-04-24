using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ForEachDbQueries.Tests;

public class ForEachDbRunnerIntegrationTests
{
    private const string PgUsername = "postgres";
    private const string PgPassword = "postgres";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("postgres")
        .WithUsername(PgUsername)
        .WithPassword(PgPassword)
        .Build();

    private string _connectionString = default!;
    private IReadOnlyList<string> _databases = default!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        await using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var names = new[] { "runner_alpha", "runner_beta", "runner_gamma" };
        foreach (var name in names)
        {
            await using var cmd = new Npgsql.NpgsqlCommand($"CREATE DATABASE {name}", connection);
            await cmd.ExecuteNonQueryAsync();
        }

        _databases = names;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _postgres.StopAsync();
    }

    [Test]
    public async Task RunQueryAsync_ShouldEmitPendingBeforeRunningForEveryDatabase()
    {
        var runner = new ForEachDbRunner(_connectionString);
        var events = new ConcurrentQueue<DatabaseStatus>();
        var progress = new Progress<DatabaseStatus>(s => events.Enqueue(s));

        await runner.RunQueryAsync(_databases, "SELECT 1;", numberOfThreads: 1, progress: progress);

        // Drain the Progress<T> queue — Progress marshals asynchronously
        await WaitForCountAsync(events, _databases.Count * 3);

        var byDb = events.GroupBy(e => e.DatabaseName).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var db in _databases)
        {
            byDb.Should().ContainKey(db);
            var states = byDb[db].Select(s => s.State).ToList();
            states[0].Should().Be(DatabaseRunState.Pending);
            states.Should().Contain(DatabaseRunState.Running);
            states[^1].Should().Be(DatabaseRunState.Succeeded);
        }
    }

    [Test]
    public async Task RunQueryAsync_ShouldPopulateDurationAndRowCountOnSuccess()
    {
        var runner = new ForEachDbRunner(_connectionString);
        var finals = new ConcurrentDictionary<string, DatabaseStatus>();
        var progress = new Progress<DatabaseStatus>(s =>
        {
            if (s.State is DatabaseRunState.Succeeded or DatabaseRunState.Failed or DatabaseRunState.Cancelled)
                finals[s.DatabaseName] = s;
        });

        await runner.RunQueryAsync<int>(_databases, "SELECT 1 AS n;", numberOfThreads: 2, progress: progress);

        await WaitForCountAsync(finals, _databases.Count);

        foreach (var db in _databases)
        {
            finals[db].State.Should().Be(DatabaseRunState.Succeeded);
            finals[db].Duration.Should().NotBeNull();
            finals[db].RowCount.Should().Be(1);
        }
    }

    [Test]
    public async Task RunQueryAsync_ShouldRouteLogEntriesToSink()
    {
        var runner = new ForEachDbRunner(_connectionString);
        var sink = new CollectingLogSink();

        await runner.RunQueryAsync(_databases, "SELECT 1;", numberOfThreads: 2, logSink: sink);

        foreach (var db in _databases)
        {
            var entries = sink.ForDatabase(db);
            entries.Should().NotBeEmpty();
            entries.First().Message.Should().Be("Query started");
            entries.Last().Message.Should().StartWith("Completed in");
            entries.Last().Level.Should().Be(DatabaseLogLevel.Info);
        }
    }

    [Test]
    public async Task RunQueryAsync_OnQueryFailure_ShouldEmitFailedStatusAndErrorLog()
    {
        var runner = new ForEachDbRunner(_connectionString);
        var finals = new ConcurrentDictionary<string, DatabaseStatus>();
        var sink = new CollectingLogSink();
        var progress = new Progress<DatabaseStatus>(s =>
        {
            if (s.State is DatabaseRunState.Succeeded or DatabaseRunState.Failed or DatabaseRunState.Cancelled)
                finals[s.DatabaseName] = s;
        });

        await runner.RunQueryAsync(_databases, "SELECT not_a_column_zzz;", numberOfThreads: 2, progress: progress, logSink: sink);

        await WaitForCountAsync(finals, _databases.Count);

        foreach (var db in _databases)
        {
            finals[db].State.Should().Be(DatabaseRunState.Failed);
            finals[db].ErrorMessage.Should().NotBeNullOrEmpty();
            sink.ForDatabase(db).Should().Contain(e => e.Level == DatabaseLogLevel.Error);
        }
    }

    [Test]
    public async Task RunQueryAsync_ShouldCaptureNoticeMessages()
    {
        var runner = new ForEachDbRunner(_connectionString);
        var sink = new CollectingLogSink();

        var raise = @"DO $$ BEGIN RAISE NOTICE 'hello from %', current_database(); END $$;";
        await runner.RunQueryAsync(_databases.Take(1), raise, numberOfThreads: 1, logSink: sink);

        var entries = sink.ForDatabase(_databases[0]);
        entries.Should().Contain(e => e.Level == DatabaseLogLevel.Notice && e.Message.Contains("hello from"));
    }

    [Test]
    public async Task RunQueryAsync_WhenCancelled_ShouldReportCancelledForInFlightAndLeavePendingOthers()
    {
        var runner = new ForEachDbRunner(_connectionString);
        var statuses = new ConcurrentDictionary<string, DatabaseStatus>();
        var firstRunning = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var progress = new Progress<DatabaseStatus>(s =>
        {
            statuses[s.DatabaseName] = s;
            if (s.State == DatabaseRunState.Running)
                firstRunning.TrySetResult(s.DatabaseName);
        });

        using var cts = new CancellationTokenSource();

        var task = runner.RunQueryAsync(
            _databases,
            "SELECT pg_sleep(10);",
            numberOfThreads: 1,
            progress: progress,
            cancellationToken: cts.Token);

        // Deterministic: wait on the explicit Running signal before cancelling — no wall-clock race.
        var runningDb = await firstRunning.Task.WaitAsync(TimeSpan.FromSeconds(30));
        cts.Cancel();

        try { await task; }
        catch (OperationCanceledException) { /* expected from the cooperatively cancelled Parallel.ForEachAsync */ }

        // Progress<T> marshals asynchronously; wait until the cancelled status has landed.
        await WaitForAsync(() => statuses.TryGetValue(runningDb, out var s) && s.State == DatabaseRunState.Cancelled);

        statuses[runningDb].State.Should().Be(DatabaseRunState.Cancelled);
        statuses.Values.Count(s => s.State == DatabaseRunState.Succeeded).Should().Be(0);
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!condition() && Environment.TickCount64 < deadline)
            await Task.Delay(25);
    }

    [Test]
    public async Task RunQueryAsDynamicAsync_ShouldReturnRowPerDatabaseWithValues()
    {
        var runner = new ForEachDbRunner(_connectionString);

        var rows = await runner.RunQueryAsDynamicAsync(
            _databases,
            "SELECT current_database() AS db_name, 1 AS answer;",
            numberOfThreads: 2);

        rows.Should().HaveCount(_databases.Count);
        rows.Select(r => r.Database).Should().BeEquivalentTo(_databases);

        foreach (var row in rows)
        {
            row.Values.Should().ContainKey("db_name");
            row.Values.Should().ContainKey("answer");
            row.Values["db_name"].Should().Be(row.Database);
        }
    }

    private static async Task WaitForCountAsync<T>(IReadOnlyCollection<T> collection, int expected, int timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (collection.Count < expected && Environment.TickCount64 < deadline)
        {
            await Task.Delay(25);
        }
    }

    private sealed class CollectingLogSink : IDatabaseLogSink
    {
        private readonly ConcurrentBag<DatabaseLogEntry> _entries = new();

        public void Append(DatabaseLogEntry entry) => _entries.Add(entry);

        public IReadOnlyList<DatabaseLogEntry> ForDatabase(string database) =>
            _entries.Where(e => e.DatabaseName == database).OrderBy(e => e.Timestamp).ToList();
    }
}
