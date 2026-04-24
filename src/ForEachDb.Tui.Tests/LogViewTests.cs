using AwesomeAssertions;
using ForEachDb.Tui.Views;
using ForEachDbQueries;
using NUnit.Framework;

namespace ForEachDb.Tui.Tests;

[NonParallelizable]
public class LogViewTests
{
    private TuiHarness _harness = null!;

    [SetUp]
    public void SetUp() => _harness = TuiHarness.Start();

    [TearDown]
    public void TearDown() => _harness.Dispose();

    [Test]
    public void Filter_StartsAtAll()
    {
        using var view = new LogView();
        view.Filter.Should().Be(LogFilter.All);
    }

    [Test]
    public void SetFilter_UpdatesTitle()
    {
        using var view = new LogView();

        view.SetFilter(LogFilter.FailedOnly);
        view.Title.ToString().Should().Be("Log (failed only)");

        view.SetSelectedDatabase("alpha");
        view.SetFilter(LogFilter.SelectedDatabase);
        view.Title.ToString().Should().Be("Log (alpha)");

        view.SetFilter(LogFilter.All);
        view.Title.ToString().Should().Be("Log (all)");
    }

    [Test]
    public void FailedOnlyFilter_KeepsAmbientAndErrors_DropsInfo()
    {
        using var view = new LogView();

        view.Append("connected to cluster"); // ambient
        view.Append(new DatabaseLogEntry("alpha", DateTimeOffset.UtcNow, DatabaseLogLevel.Info, "started"));
        view.Append(new DatabaseLogEntry("alpha", DateTimeOffset.UtcNow, DatabaseLogLevel.Error, "boom"));
        view.Append(new DatabaseLogEntry("beta", DateTimeOffset.UtcNow, DatabaseLogLevel.Notice, "auto-vacuum"));

        view.SetFilter(LogFilter.FailedOnly);

        var rendered = view.VisibleTextForTests;
        rendered.Should().Contain("connected to cluster");
        rendered.Should().Contain("boom");
        rendered.Should().NotContain("started");
        rendered.Should().NotContain("auto-vacuum");
    }

    [Test]
    public void SelectedDatabaseFilter_ShowsOnlyMatchingDb_PlusAmbient()
    {
        using var view = new LogView();

        view.Append("welcome");
        view.Append(new DatabaseLogEntry("alpha", DateTimeOffset.UtcNow, DatabaseLogLevel.Info, "one"));
        view.Append(new DatabaseLogEntry("beta", DateTimeOffset.UtcNow, DatabaseLogLevel.Info, "two"));

        view.SetSelectedDatabase("alpha");
        view.SetFilter(LogFilter.SelectedDatabase);

        var rendered = view.VisibleTextForTests;
        rendered.Should().Contain("welcome");
        rendered.Should().Contain("one");
        rendered.Should().NotContain("two");
    }
}
