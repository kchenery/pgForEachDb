using AwesomeAssertions;
using ForEachDb.Tui.Views;
using ForEachDbQueries;
using NUnit.Framework;

namespace ForEachDb.Tui.Tests;

[NonParallelizable]
public class DatabaseListViewTests
{
    private TuiHarness _harness = null!;

    [SetUp]
    public void SetUp() => _harness = TuiHarness.Start();

    [TearDown]
    public void TearDown() => _harness.Dispose();

    [Test]
    public void AllDatabases_StartSelected()
    {
        using var view = new DatabaseListView(new[] { "a", "b", "c" });

        view.SelectedCount.Should().Be(3);
        view.SelectedDatabases.Should().Equal("a", "b", "c");
        view.TotalCount.Should().Be(3);
    }

    [Test]
    public void SelectNone_ClearsSelection()
    {
        using var view = new DatabaseListView(new[] { "a", "b", "c" });

        view.SelectNone();

        view.SelectedCount.Should().Be(0);
        view.SelectedDatabases.Should().BeEmpty();
    }

    [Test]
    public void SetSelection_IntersectsWithKnownDatabases()
    {
        using var view = new DatabaseListView(new[] { "alpha", "beta", "gamma" });

        view.SetSelection(new[] { "beta", "delta", "gamma" });

        view.SelectedDatabases.Should().Equal("beta", "gamma");
    }

    [Test]
    public void SelectionChanged_FiresWhenSelectNoneCalled()
    {
        using var view = new DatabaseListView(new[] { "a", "b" });

        var fired = 0;
        view.SelectionChanged += () => fired++;

        view.SelectNone();

        fired.Should().Be(1);
    }

    [Test]
    public void ApplyStatus_TracksLastStatusByDatabase()
    {
        using var view = new DatabaseListView(new[] { "alpha", "beta" });

        view.ApplyStatus(new DatabaseStatus("alpha", DatabaseRunState.Running));
        view.ApplyStatus(new DatabaseStatus("alpha", DatabaseRunState.Succeeded, Duration: TimeSpan.FromMilliseconds(250)));
        view.ApplyStatus(new DatabaseStatus("beta", DatabaseRunState.Failed, ErrorMessage: "boom"));

        view.Statuses["alpha"].State.Should().Be(DatabaseRunState.Succeeded);
        view.Statuses["alpha"].Duration.Should().NotBeNull();
        view.Statuses["beta"].State.Should().Be(DatabaseRunState.Failed);
        view.Statuses["beta"].ErrorMessage.Should().Be("boom");
    }

    [Test]
    public void ResetStatuses_ClearsTrackedState()
    {
        using var view = new DatabaseListView(new[] { "alpha" });
        view.ApplyStatus(new DatabaseStatus("alpha", DatabaseRunState.Succeeded));

        view.ResetStatuses();

        view.Statuses.Should().BeEmpty();
    }
}
