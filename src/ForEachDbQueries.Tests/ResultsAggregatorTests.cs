using System.Collections.Generic;
using AwesomeAssertions;
using NUnit.Framework;

namespace ForEachDbQueries.Tests;

public class ResultsAggregatorTests
{
    [Test]
    public void Aggregate_ShouldEmitDatabaseAsFirstColumn()
    {
        var source = new[]
        {
            Row("alpha", ("count", 1)),
            Row("beta", ("count", 2))
        };

        var result = ResultsAggregator.Aggregate(source);

        result.Columns.Should().Equal("database", "count");
        result.Rows.Should().HaveCount(2);
        result.Rows[0].Should().Equal("alpha", 1);
        result.Rows[1].Should().Equal("beta", 2);
    }

    [Test]
    public void Aggregate_WithMismatchedColumns_ShouldUnionAndFillNull()
    {
        var source = new[]
        {
            Row("alpha", ("a", 1)),
            Row("beta", ("b", 2))
        };

        var result = ResultsAggregator.Aggregate(source);

        result.Columns.Should().Equal("database", "a", "b");
        result.Rows[0].Should().Equal("alpha", 1, null);
        result.Rows[1].Should().Equal("beta", null, 2);
    }

    [Test]
    public void Aggregate_ShouldPreserveFirstSeenColumnOrder()
    {
        var source = new[]
        {
            Row("alpha", ("b", 1), ("a", 2)),
            Row("beta", ("c", 3), ("a", 4))
        };

        var result = ResultsAggregator.Aggregate(source);

        result.Columns.Should().Equal("database", "b", "a", "c");
    }

    [Test]
    public void Aggregate_WithEmptySource_ShouldStillContainDatabaseColumn()
    {
        var result = ResultsAggregator.Aggregate([]);

        result.Columns.Should().Equal("database");
        result.Rows.Should().BeEmpty();
    }

    private static DatabaseRow Row(string database, params (string Key, object? Value)[] values)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var (key, value) in values) dict[key] = value;
        return new DatabaseRow(database, dict);
    }
}
