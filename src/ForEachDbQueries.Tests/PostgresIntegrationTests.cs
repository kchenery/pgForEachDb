using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace ForEachDbQueries.Tests;

public class PostgresIntegrationTests
{
    private NpgsqlConnection? _pgConn;

    [SetUp]
    public void Setup()
    {
        var csBuilder = new NpgsqlConnectionStringBuilder
        {
            Database = "postgres",
            Host = "localhost",
            Username = "postgres",
            Password = "postgres"
        };

        _pgConn = new NpgsqlConnection(csBuilder.ToString());
    }

    [Test]
    public async Task ForEachDbQuery_ShouldFindDatabases()
    {
        // Arrange
        var query = new DatabaseFinder().Query();
        
        // Act
        var results = (await _pgConn.QueryAsync<string>(query.RawSql, query.Parameters)).ToList();
        
        // Assert
        results.Count.Should().BePositive();
        results.Should().NotContain("template0");
        results.Should().Contain("template1");
        results.Should().Contain("postgres");
    }
    
    [Test]
    public async Task ForEachDbQuery_WithIgnorePostgres_ShouldFindDatabases()
    {
        // Arrange
        var query = new DatabaseFinder().IgnorePostgresDb().Query();
        
        // Act
        var results = (await _pgConn.QueryAsync<string>(query.RawSql, query.Parameters)).ToList();
        
        // Assert
        results.Count.Should().BePositive();
        results.Should().NotContain("template0");
        results.Should().Contain("template1");
        results.Should().NotContain("postgres");
    }
    
    [Test]
    public async Task ForEachDbQuery_WithIgnorePgTemplates_ShouldNotContainTemplateDbs()
    {
        // Arrange
        var query = new DatabaseFinder().IgnoreTemplateDb().Query();
        
        // Act
        var results = (await _pgConn.QueryAsync<string>(query.RawSql, query.Parameters)).ToList();
        
        // Assert
        results.Count.Should().BePositive();
        results.Should().NotContain("template0");
        results.Should().NotContain("template1");
    }
}