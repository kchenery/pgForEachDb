using System.Collections.Concurrent;
using ForEachDbQueries;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ForEachDb;

public sealed class LiveProgressRenderer : IProgress<DatabaseStatus>, IDisposable
{
    private static readonly string[] DotFrames = ["\u2840", "\u2844", "\u2846", "\u2847", "\u2807", "\u2803", "\u2801", "\u2809", "\u2819", "\u2838", "\u2830", "\u2820"];

    private readonly LiveDisplayContext _ctx;
    private readonly ConcurrentDictionary<string, DatabaseStatus> _statuses = new();
    private readonly int _totalDatabases;
    private readonly Timer _animationTimer;
    private int _frame;

    public LiveProgressRenderer(LiveDisplayContext ctx, int totalDatabases)
    {
        _ctx = ctx;
        _totalDatabases = totalDatabases;
        _animationTimer = new Timer(_ =>
        {
            Interlocked.Increment(ref _frame);
            _ctx.UpdateTarget(BuildDisplay());
        }, null, TimeSpan.FromMilliseconds(80), TimeSpan.FromMilliseconds(80));
    }

    public void Report(DatabaseStatus value)
    {
        _statuses[value.DatabaseName] = value;
        _ctx.UpdateTarget(BuildDisplay());
    }

    public IRenderable BuildDisplay()
    {
        var rows = new List<IRenderable>();
        var dots = DotFrames[Volatile.Read(ref _frame) % DotFrames.Length];

        var completed = _statuses.Values
            .Where(s => s.State is DatabaseRunState.Succeeded or DatabaseRunState.Failed or DatabaseRunState.Cancelled)
            .OrderBy(s => s.DatabaseName)
            .ToList();

        var running = _statuses.Values
            .Where(s => s.State == DatabaseRunState.Running)
            .OrderBy(s => s.DatabaseName)
            .ToList();

        foreach (var db in completed)
        {
            rows.Add(db.State switch
            {
                DatabaseRunState.Succeeded => new Markup($"[green]\u2714[/] {Markup.Escape(db.DatabaseName)}"),
                DatabaseRunState.Cancelled => new Markup($"[dim]\u25cb {Markup.Escape(db.DatabaseName)} - cancelled[/]"),
                _ => new Markup($"[red]\u2718[/] {Markup.Escape(db.DatabaseName)} [dim]- {Markup.Escape(Sanitize(db.ErrorMessage ?? "Unknown error", 80))}[/]")
            });
        }

        if (completed.Count > 0 && running.Count > 0)
        {
            rows.Add(new Rule().RuleStyle(Style.Parse("dim")));
        }

        foreach (var db in running)
        {
            rows.Add(new Markup($"[yellow]{dots}[/] {Markup.Escape(db.DatabaseName)}"));
        }

        if (rows.Count > 0)
        {
            rows.Add(Text.Empty);
        }

        rows.Add(new Markup($"[bold]{completed.Count}[/] / [bold]{_totalDatabases}[/] databases completed"));

        return new Rows(rows);
    }

    public void Dispose()
    {
        _animationTimer.Dispose();
    }

    private static string Sanitize(string value, int maxLength)
    {
        var oneLine = value.ReplaceLineEndings(" ");
        return oneLine.Length <= maxLength ? oneLine : string.Concat(oneLine.AsSpan(0, maxLength), "...");
    }
}
