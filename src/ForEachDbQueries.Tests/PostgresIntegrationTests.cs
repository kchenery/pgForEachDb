using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;
using ForEachDbQueries.DapperExtensions;

namespace ForEachDbQueries.Tests;

public class PostgresIntegrationTests
{
    private const string PgDatabase = "ignored";
    private const string PgUsername = "postgres";
    private const string PgPassword = "postgres";
    
    private NpgsqlConnection _pgConn = default!;

    private readonly PostgreSqlContainer _postgresqlContainer = new PostgreSqlBuilder()
        .WithDatabase(PgDatabase)
        .WithUsername(PgUsername)
        .WithPassword(PgPassword)
        .Build();
    
    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await _postgresqlContainer.StartAsync();
        _pgConn = new NpgsqlConnection(_postgresqlContainer.GetConnectionString());
        await _pgConn.ExecuteAsync("CREATE DATABASE foo");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _postgresqlContainer.StopAsync();
    }
    
    [SetUp]
    public void Setup()
    {
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

    [Test]
    [TestCase(true, "ASC")]
    [TestCase(false, "DESC")]
    public void ForEachDbQuery_WithOrderByName_ShouldContainOrderByClause(bool ascending, string direction)
    {
        // Arrange
        var query = new DatabaseFinder().OrderByName(ascending).Query();
        
        // Assert
        query.RawSql.Should().Contain("ORDER BY");
        query.RawSql.Trim().Should().EndWith(direction);
    }

    [Test]
    public void ForEachDbQuery_WithIncludeUnconnectableDatabases_ShouldContain()
    {
        // Arrange
        var databaseFinder = new DatabaseFinder().IncludeUnconnectableDatabases();
        
        // Assert
        databaseFinder.Query().RawSql.Should().NotContain("datallowconn = true");
    }

    [Test]
    public async Task ForEachDbQuery_WithIgnoreDatabaseIgnored_ShouldNotReturnDatabase()
    {
        // Arrange
        var ignore = new DatabaseFinder().IgnoreDatabase("ignored").Query();
        var include = new DatabaseFinder().Query();

        // Act
        var ignoreResults = (await _pgConn.QueryAsync<string>(ignore.RawSql, ignore.Parameters)).ToList();
        var includeResults = (await _pgConn.QueryAsync<string>(include.RawSql, include.Parameters)).ToList();
        
        // Assert
        ignoreResults.Should().NotContain("ignored");
        includeResults.Should().Contain("ignored");
    }

    [TestCase(new object[] {})]
    [TestCase(new object[] {"foo"})]
    [TestCase(new object[] {"foo", "bar"})]
    [TestCase(new object[] {"foo", "bar", "baz"})]
    [Test]
    public async Task ForEachDbQuery_WithIgnoreDatabases_ShouldNotReturnSuppliedDatabases(
        params string[] ignoredDatabases)
    {
        // Arrange
        var ignored = new DatabaseFinder().IgnoreDatabases(ignoredDatabases);
        
        // Act
        var ignoreResults = (await _pgConn.QueryAsync<string>(ignored)).ToList();
    }
}