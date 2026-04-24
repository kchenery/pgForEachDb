using System.IO;
using AwesomeAssertions;
using ForEachDb.Tui.Infrastructure;
using ForEachDb.Tui.Models;
using NUnit.Framework;

namespace ForEachDb.Tui.Tests;

public class RecipeStoreTests
{
    private string _tempPath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"pgForEachDb-tests-{Guid.NewGuid():N}", "recipes.json");
    }

    [TearDown]
    public void TearDown()
    {
        var directory = Path.GetDirectoryName(_tempPath);
        if (directory is not null && Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    [Test]
    public void Load_WhenFileMissing_ReturnsEmpty()
    {
        var store = new RecipeStore(_tempPath);

        store.Load().Should().BeEmpty();
    }

    [Test]
    public void SaveThenLoad_RoundTripsFields()
    {
        var store = new RecipeStore(_tempPath);
        var recipe = Sample("prod maintenance");

        store.Save(recipe);

        var loaded = store.Load();
        loaded.Should().HaveCount(1);
        loaded[0].Should().BeEquivalentTo(recipe);
    }

    [Test]
    public void Save_WithSameName_Overwrites()
    {
        var store = new RecipeStore(_tempPath);
        store.Save(Sample("prod") with { Query = "ANALYZE;" });
        store.Save(Sample("prod") with { Query = "VACUUM;" });

        var loaded = store.Load();
        loaded.Should().HaveCount(1);
        loaded[0].Query.Should().Be("VACUUM;");
    }

    [Test]
    public void Save_NameComparisonIsCaseInsensitive()
    {
        var store = new RecipeStore(_tempPath);
        store.Save(Sample("Prod") with { Query = "A;" });
        store.Save(Sample("PROD") with { Query = "B;" });

        store.Load().Should().HaveCount(1);
    }

    [Test]
    public void Delete_RemovesRecipeAndReportsOutcome()
    {
        var store = new RecipeStore(_tempPath);
        store.Save(Sample("keep"));
        store.Save(Sample("drop"));

        store.Delete("drop").Should().BeTrue();
        store.Delete("drop").Should().BeFalse();

        store.Load().Select(r => r.Name).Should().Equal("keep");
    }

    [Test]
    public void Exists_MatchesCaseInsensitively()
    {
        var store = new RecipeStore(_tempPath);
        store.Save(Sample("Prod"));

        store.Exists("prod").Should().BeTrue();
        store.Exists("missing").Should().BeFalse();
    }

    [Test]
    public void Load_WithCorruptFile_TreatsAsEmpty()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tempPath)!);
        File.WriteAllText(_tempPath, "{ not valid json");

        var store = new RecipeStore(_tempPath);

        store.Load().Should().BeEmpty();
    }

    private static Recipe Sample(string name) => new(
        Name: name,
        Connection: new ConnectionSettings(
            Host: "db.example",
            Port: 5432,
            Database: "postgres",
            Username: "admin",
            Password: string.Empty,
            IncludePostgresDb: false,
            IncludeTemplateDb: false,
            IgnoreDatabases: new[] { "_old" }),
        SelectedDatabases: new[] { "app", "billing" },
        Query: "ANALYZE;",
        Threads: 4);
}
