using AwesomeAssertions;
using ForEachDb.Desktop.ViewModels;
using NUnit.Framework;

namespace ForEachDb.Desktop.Tests;

public class FilterSelectorTests
{
    [Test]
    public void Default_state_has_only_the_all_sentinel_and_matches_everything()
    {
        var f = new FilterSelector();

        f.Values.Should().BeEquivalentTo(new[] { WorkspaceDefaults.AllDatabasesFilter });
        f.Selected.Should().Be(WorkspaceDefaults.AllDatabasesFilter);

        f.Matches("anything").Should().BeTrue();
        f.Matches("postgres").Should().BeTrue();
    }

    [Test]
    public void RegisterValue_adds_unique_values_in_order()
    {
        var f = new FilterSelector();
        f.RegisterValue("billing");
        f.RegisterValue("app");
        f.RegisterValue("billing"); // dup is ignored

        f.Values.Should().Equal(WorkspaceDefaults.AllDatabasesFilter, "billing", "app");
    }

    [Test]
    public void RegisterValues_adds_in_order_skipping_dups()
    {
        var f = new FilterSelector();
        f.RegisterValues(new[] { "a", "b", "a", "c" });

        f.Values.Should().Equal(WorkspaceDefaults.AllDatabasesFilter, "a", "b", "c");
    }

    [Test]
    public void Matches_with_specific_selection_returns_true_only_for_that_value()
    {
        var f = new FilterSelector();
        f.RegisterValue("app");
        f.RegisterValue("billing");
        f.Selected = "app";

        f.Matches("app").Should().BeTrue();
        f.Matches("billing").Should().BeFalse();
        f.Matches("anything").Should().BeFalse();
    }

    [Test]
    public void Reset_clears_registered_values_and_returns_to_all()
    {
        var f = new FilterSelector();
        f.RegisterValues(new[] { "a", "b" });
        f.Selected = "a";

        f.Reset();

        f.Values.Should().Equal(WorkspaceDefaults.AllDatabasesFilter);
        f.Selected.Should().Be(WorkspaceDefaults.AllDatabasesFilter);
        f.Matches("a").Should().BeTrue();
    }

    [Test]
    public void Reset_re_registers_values_from_scratch()
    {
        var f = new FilterSelector();
        f.RegisterValue("first");
        f.Reset();

        f.RegisterValue("first");

        f.Values.Should().Equal(WorkspaceDefaults.AllDatabasesFilter, "first");
    }

    [Test]
    public void Selected_change_raises_PropertyChanged()
    {
        var f = new FilterSelector();
        f.RegisterValue("app");
        var changes = new List<string?>();
        f.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        f.Selected = "app";

        changes.Should().Contain(nameof(FilterSelector.Selected));
    }
}
