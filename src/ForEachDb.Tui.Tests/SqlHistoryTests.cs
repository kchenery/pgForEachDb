using AwesomeAssertions;
using ForEachDb.Tui.Infrastructure;
using NUnit.Framework;

namespace ForEachDb.Tui.Tests;

public class SqlHistoryTests
{
    [Test]
    public void Older_OnEmpty_ReturnsNull()
    {
        var history = new SqlHistory();
        history.Older("anything").Should().BeNull();
    }

    [Test]
    public void Older_FromDraft_SavesDraftAndReturnsNewest()
    {
        var history = new SqlHistory();
        history.Push("ANALYZE;");

        history.Older("draft in progress").Should().Be("ANALYZE;");
        history.Newer().Should().Be("draft in progress");
    }

    [Test]
    public void Older_ThroughMultipleEntries_WalksBackInOrder()
    {
        var history = new SqlHistory();
        history.Push("first");
        history.Push("second");
        history.Push("third");

        history.Older("draft").Should().Be("third");
        history.Older("draft").Should().Be("second");
        history.Older("draft").Should().Be("first");
        history.Older("draft").Should().BeNull();
    }

    [Test]
    public void Newer_WalksForwardAndEndsAtDraft()
    {
        var history = new SqlHistory();
        history.Push("a");
        history.Push("b");
        history.Push("c");

        history.Older("draft");
        history.Older("draft");
        history.Older("draft");

        history.Newer().Should().Be("b");
        history.Newer().Should().Be("c");
        history.Newer().Should().Be("draft");
        history.Newer().Should().BeNull();
    }

    [Test]
    public void Push_IgnoresConsecutiveDuplicates()
    {
        var history = new SqlHistory();
        history.Push("same");
        history.Push("same");

        history.Count.Should().Be(1);
    }

    [Test]
    public void Push_IgnoresWhitespaceOnly()
    {
        var history = new SqlHistory();
        history.Push("   \n");

        history.Count.Should().Be(0);
    }

    [Test]
    public void Push_EvictsOldestOnceCapacityReached()
    {
        var history = new SqlHistory(capacity: 3);
        history.Push("one");
        history.Push("two");
        history.Push("three");
        history.Push("four");

        history.Count.Should().Be(3);
        history.Older("draft").Should().Be("four");
        history.Older("draft").Should().Be("three");
        history.Older("draft").Should().Be("two");
        history.Older("draft").Should().BeNull();
    }

    [Test]
    public void Push_ResetsNavigationIndex()
    {
        var history = new SqlHistory();
        history.Push("a");
        history.Push("b");
        history.Older("draft");

        history.Push("c");

        history.Older("newdraft").Should().Be("c");
    }
}
