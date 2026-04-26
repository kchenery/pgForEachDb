using AwesomeAssertions;
using ForEachDb.Desktop.ViewModels;
using ForEachDbQueries;
using NUnit.Framework;

namespace ForEachDb.Desktop.Tests;

public class DatabaseItemTests
{
    [Test]
    public void Toggling_IsSelected_invokes_callback()
    {
        var calls = 0;
        var item = new DatabaseItem("app", isTemplate: false, isSelected: false, () => calls++);

        item.IsSelected = true;
        item.IsSelected = false;

        calls.Should().Be(2);
    }

    [Test]
    public void Setting_IsSelected_to_same_value_does_not_invoke_callback()
    {
        var calls = 0;
        var item = new DatabaseItem("app", isTemplate: false, isSelected: false, () => calls++);

        item.IsSelected = false;

        calls.Should().Be(0);
    }

    [Test]
    public void RunState_Pending_at_construction_means_no_status_to_show()
    {
        var item = new DatabaseItem("app", isTemplate: false, isSelected: false, () => { });
        item.IsPending.Should().BeTrue();
        item.HasStatus.Should().BeFalse();
        item.IsTerminal.Should().BeFalse();
    }

    [TestCase(DatabaseRunState.Running,   "running",   false)]
    [TestCase(DatabaseRunState.Succeeded, "done",      true)]
    [TestCase(DatabaseRunState.Failed,    "failed",    true)]
    [TestCase(DatabaseRunState.Cancelled, "cancelled", true)]
    public void ApplyStatus_updates_badge_text_and_terminal_flag(
        DatabaseRunState state, string expectedBadge, bool expectedTerminal)
    {
        var item = new DatabaseItem("app", isTemplate: false, isSelected: true, () => { });

        item.ApplyStatus(new DatabaseStatus("app", state));

        item.BadgeText.Should().Be(expectedBadge);
        item.IsTerminal.Should().Be(expectedTerminal);
        item.HasStatus.Should().BeTrue();
    }

    [Test]
    public void Reset_returns_state_to_Pending()
    {
        var item = new DatabaseItem("app", isTemplate: false, isSelected: true, () => { });
        item.ApplyStatus(new DatabaseStatus("app", DatabaseRunState.Failed));

        item.Reset();

        item.IsPending.Should().BeTrue();
        item.HasStatus.Should().BeFalse();
    }

    [Test]
    public void IsTemplate_is_immutable_after_construction()
    {
        var item = new DatabaseItem("template1", isTemplate: true, isSelected: false, () => { });
        item.IsTemplate.Should().BeTrue();
    }
}
