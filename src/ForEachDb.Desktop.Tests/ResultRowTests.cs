using AwesomeAssertions;
using ForEachDb.Desktop.ViewModels;
using NUnit.Framework;

namespace ForEachDb.Desktop.Tests;

public class ResultRowTests
{
    [Test]
    public void Cells_align_with_columns_in_order()
    {
        var columns = new[] { "database", "size_mb", "rows" };
        var values  = new object?[] { "app", 12.5, 42 };

        var row = new ResultRow(columns, values);

        row.Cells.Should().HaveCount(3);
        row.Cells[0].Column.Should().Be("database");
        row.Cells[0].Value.Should().Be("app");
        row.Cells[1].Column.Should().Be("size_mb");
        row.Cells[1].Value.Should().Be("12.5");
        row.Cells[2].Column.Should().Be("rows");
        row.Cells[2].Value.Should().Be("42");
    }

    [Test]
    public void Null_value_becomes_empty_string()
    {
        var row = new ResultRow(new[] { "x" }, new object?[] { null });

        row.Cells.Single().Value.Should().Be(string.Empty);
    }

    [Test]
    public void Empty_columns_yields_empty_cells()
    {
        var row = new ResultRow(Array.Empty<string>(), Array.Empty<object?>());
        row.Cells.Should().BeEmpty();
    }
}
