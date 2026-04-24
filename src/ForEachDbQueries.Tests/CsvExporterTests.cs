using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AwesomeAssertions;
using NUnit.Framework;

namespace ForEachDbQueries.Tests;

public class CsvExporterTests
{
    [Test]
    public async Task WriteAsync_ShouldPutDatabaseAsFirstColumn()
    {
        var rows = new[]
        {
            Row("alpha", ("count", 1)),
            Row("beta", ("count", 2))
        };

        var csv = await ExportAsync(rows);

        csv.Should().Be("database,count\r\nalpha,1\r\nbeta,2\r\n".Replace("\r\n", System.Environment.NewLine));
    }

    [Test]
    public async Task WriteAsync_WithMismatchedColumns_ShouldUnionAndBlankMissing()
    {
        var rows = new[]
        {
            Row("alpha", ("a", "x")),
            Row("beta", ("b", "y"))
        };

        var csv = await ExportAsync(rows);
        var lines = csv.Split(System.Environment.NewLine, System.StringSplitOptions.RemoveEmptyEntries);

        lines[0].Should().Be("database,a,b");
        lines[1].Should().Be("alpha,x,");
        lines[2].Should().Be("beta,,y");
    }

    [Test]
    public async Task WriteAsync_ShouldQuoteFieldsContainingSpecialCharacters()
    {
        var rows = new[]
        {
            Row("alpha", ("text", "has, comma")),
            Row("beta", ("text", "has \"quote\"")),
            Row("gamma", ("text", "line\nbreak"))
        };

        var csv = await ExportAsync(rows);
        var lines = csv.Split(System.Environment.NewLine, System.StringSplitOptions.None);

        lines[0].Should().Be("database,text");
        lines[1].Should().Be("alpha,\"has, comma\"");
        lines[2].Should().Be("beta,\"has \"\"quote\"\"\"");
        // embedded newline splits across two underlying lines; reassemble
        lines[3].Should().Be("gamma,\"line");
        lines[4].Should().Be("break\"");
    }

    [Test]
    public async Task WriteAsync_WithNullValues_ShouldEmitEmptyField()
    {
        var rows = new[]
        {
            Row("alpha", ("val", (object?)null))
        };

        var csv = await ExportAsync(rows);
        var lines = csv.Split(System.Environment.NewLine, System.StringSplitOptions.RemoveEmptyEntries);

        lines[1].Should().Be("alpha,");
    }

    [Test]
    public void BuildColumnOrder_ShouldPreserveFirstSeenOrder()
    {
        var rows = new[]
        {
            Row("alpha", ("b", 1), ("a", 2)),
            Row("beta", ("c", 3), ("a", 4))
        };

        var columns = CsvExporter.BuildColumnOrder(rows);

        columns.Should().Equal("database", "b", "a", "c");
    }

    private static DatabaseRow Row(string database, params (string Key, object? Value)[] values)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var (key, value) in values) dict[key] = value;
        return new DatabaseRow(database, dict);
    }

    private static async Task<string> ExportAsync(IEnumerable<DatabaseRow> rows)
    {
        using var ms = new MemoryStream();
        await CsvExporter.WriteAsync(ms, rows);
        ms.Position = 0;
        using var reader = new StreamReader(ms);
        return await reader.ReadToEndAsync();
    }
}
