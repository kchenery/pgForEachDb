namespace ForEachDbQueries.Tests;

using FluentAssertions;
using NUnit.Framework;

public class ParametersExtensionsTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void EmptyQueryParameters_AsDictionary_ShouldBeEmpty()
    {
        // Arrange
        var dfq = new DatabaseFinder().Query();
        
        // Assert
        dfq.Parameters.AsDynamicParameters().AsDictionary().Should().BeEmpty();
    }
    
    [Test]
    public void SingleQueryParameter_AsDictionary_ShouldHaveValue()
    {
        // Arrange
        var dfq = new DatabaseFinder().IgnorePostgresDb().Query();
        
        // Assert
        dfq.Parameters.AsDynamicParameters().AsDictionary().Should().NotBeEmpty();
        dfq.Parameters.AsDynamicParameters().AsDictionary().Should().ContainKey("database1");
    }
    
    [Test]
    public void MultipleQueryParameter_AsDictionary_ShouldHaveValue()
    {
        // Arrange
        var dfq = new DatabaseFinder().IgnorePostgresDb().IgnoreDatabase("foo").Query();
        
        // Assert
        dfq.Parameters.AsDynamicParameters().AsDictionary().Should().NotBeEmpty();
        dfq.Parameters.AsDynamicParameters().AsDictionary().Should().ContainKey("database1");
        dfq.Parameters.AsDynamicParameters().AsDictionary().Should().ContainKey("database2");
    }
}