using FluentAssertions;
using NUnit.Framework;

namespace ForEachDbQueries.Tests;

public class ForEachDbQueriesTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ForEachDbQuery_WithMultipleIgnoreDatabases_ShouldParameteriseIndividually()
    {
        // Arrange
        var expectedSql = "SELECT datname FROM pg_database WHERE datname != @database1 AND datname != @database2 AND datallowconn = true";
        
        var dbFinder = new DatabaseFinder()
            .IgnoreDatabase("foo")
            .IgnoreDatabase("bar");
        
        // Act
        var query = dbFinder.Query();
        
        // Assert
        query.RawSql.Trim().Should().Match(expectedSql);
    }

    [Test]
    public void ForEachDbQuery_WithIgnoreTemplateDatabases_ShouldFilterOutTemplateDatabases()
    {
        // Arrange
        var expectedSql = "SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true";
        
        var dbFinder = new DatabaseFinder()
            .IgnoreTemplateDb();
        
        // Act
        var query = dbFinder.Query();
        
        // Assert
        query.RawSql.Trim().Should().Match(expectedSql);
    }

    [Test]
    public void ForEachDbQuery_WithIgnorePostgresDb_ShouldFilterOutPostgresDb()
    {
        // Arrange
        var expectedSql = "SELECT datname FROM pg_database WHERE datname != @postgres AND datallowconn = true";
        
        var dbFinder = new DatabaseFinder();
        dbFinder.IgnorePostgresDb();
        
        // Act
        var query = dbFinder.Query();
        
        // Assert
        query.RawSql.Trim().Should().Match(expectedSql);
        
    }
}