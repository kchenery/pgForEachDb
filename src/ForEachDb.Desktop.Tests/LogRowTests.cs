using AwesomeAssertions;
using ForEachDb.Desktop.ViewModels;
using ForEachDbQueries;
using NUnit.Framework;

namespace ForEachDb.Desktop.Tests;

public class LogRowTests
{
    private static DatabaseLogEntry Entry(DatabaseLogLevel level) =>
        new("app_db", new DateTimeOffset(2026, 4, 26, 9, 30, 15, TimeSpan.Zero), level, "boom");

    [Test]
    public void Info_entry_sets_only_IsInfo()
    {
        var row = new LogRow(Entry(DatabaseLogLevel.Info));

        row.IsInfo.Should().BeTrue();
        row.IsNotice.Should().BeFalse();
        row.IsWarning.Should().BeFalse();
        row.IsError.Should().BeFalse();
        row.Level.Should().Be("INFO");
    }

    [Test]
    public void Notice_entry_sets_only_IsNotice()
    {
        var row = new LogRow(Entry(DatabaseLogLevel.Notice));
        row.IsNotice.Should().BeTrue();
        row.IsInfo.Should().BeFalse();
        row.Level.Should().Be("NOTICE");
    }

    [Test]
    public void Warning_entry_sets_only_IsWarning()
    {
        var row = new LogRow(Entry(DatabaseLogLevel.Warning));
        row.IsWarning.Should().BeTrue();
        row.Level.Should().Be("WARNING");
    }

    [Test]
    public void Error_entry_sets_only_IsError()
    {
        var row = new LogRow(Entry(DatabaseLogLevel.Error));
        row.IsError.Should().BeTrue();
        row.Level.Should().Be("ERROR");
    }

    [Test]
    public void Database_and_message_are_copied_through()
    {
        var row = new LogRow(Entry(DatabaseLogLevel.Info));
        row.Database.Should().Be("app_db");
        row.Message.Should().Be("boom");
    }

    [Test]
    public void Newly_constructed_row_is_visible_by_default()
    {
        var row = new LogRow(Entry(DatabaseLogLevel.Info));
        row.IsVisible.Should().BeTrue();
    }
}
