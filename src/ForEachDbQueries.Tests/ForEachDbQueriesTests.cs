using System.Collections.Generic;
using System.Linq;
using Dapper;
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
        var parameters = (DynamicParameters) query.Parameters;
        var paramDatabases = new List<string>();
        foreach (var paramName in parameters.ParameterNames)
        {
            paramDatabases.Add(parameters.Get<dynamic>(paramName));
        }

        // Assert
        query.RawSql.Trim().Should().Match(expectedSql);
        query.Parameters.AsDynamicParameters().ValueList().Where(p => p is string).Should().Contain("foo");
        query.Parameters.AsDynamicParameters().ValueList().Where(p => p is string).Should().Contain("bar");
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
        var expectedSql = "SELECT datname FROM pg_database WHERE datname != @database1 AND datallowconn = true";
        
        var dbFinder = new DatabaseFinder();
        dbFinder.IgnorePostgresDb();
        
        // Act
        var query = dbFinder.Query();
        
        // Assert
        query.RawSql.Trim().Should().Match(expectedSql);
        
    }
}