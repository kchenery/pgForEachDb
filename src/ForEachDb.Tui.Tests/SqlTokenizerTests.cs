using AwesomeAssertions;
using ForEachDb.Tui.Infrastructure;
using NUnit.Framework;

namespace ForEachDb.Tui.Tests;

public class SqlTokenizerTests
{
    [Test]
    public void Tokenize_BasicSelect_ClassifiesKeywordsAndIdentifiers()
    {
        var text = "SELECT id FROM users";

        var tokens = Classify(text);

        tokens.Should().Equal(
            (SqlTokenKind.Keyword, "SELECT"),
            (SqlTokenKind.Whitespace, " "),
            (SqlTokenKind.Identifier, "id"),
            (SqlTokenKind.Whitespace, " "),
            (SqlTokenKind.Keyword, "FROM"),
            (SqlTokenKind.Whitespace, " "),
            (SqlTokenKind.Identifier, "users"));
    }

    [Test]
    public void Tokenize_RecognisesLineAndBlockComments()
    {
        var text = "-- a line\nSELECT /* block */ 1";

        var tokens = Classify(text);

        tokens.Should().Contain((SqlTokenKind.Comment, "-- a line"));
        tokens.Should().Contain((SqlTokenKind.Comment, "/* block */"));
    }

    [Test]
    public void Tokenize_HandlesSingleQuotedStringsWithEscapedQuotes()
    {
        var text = "SELECT 'it''s fine'";

        var tokens = Classify(text);

        tokens.Should().ContainInOrder(
            (SqlTokenKind.Keyword, "SELECT"),
            (SqlTokenKind.Whitespace, " "),
            (SqlTokenKind.String, "'it''s fine'"));
    }

    [Test]
    public void Tokenize_HandlesDollarQuotedStrings()
    {
        var text = "DO $$ BEGIN RAISE NOTICE 'hi'; END $$;";

        var tokens = Classify(text);

        tokens.Should().Contain(t => t.Kind == SqlTokenKind.String && t.Value.StartsWith("$$") && t.Value.EndsWith("$$"));
    }

    [Test]
    public void Tokenize_HandlesTaggedDollarQuotes()
    {
        var text = "SELECT $body$ a 'quote' b $body$;";

        var tokens = Classify(text);

        tokens.Should().ContainSingle(t =>
            t.Kind == SqlTokenKind.String &&
            t.Value.StartsWith("$body$") &&
            t.Value.EndsWith("$body$"));
    }

    [Test]
    public void Tokenize_RecognisesDecimalNumbers()
    {
        var text = "WHERE total > 10.5";

        var tokens = Classify(text);

        tokens.Should().Contain((SqlTokenKind.Number, "10.5"));
    }

    [Test]
    public void Tokenize_UnterminatedString_ConsumesToEndOfInput()
    {
        var text = "SELECT 'never closed";

        var tokens = Classify(text);

        tokens[^1].Kind.Should().Be(SqlTokenKind.String);
        tokens[^1].Value.Should().Be("'never closed");
    }

    [Test]
    public void Tokenize_Empty_ReturnsNoTokens()
    {
        SqlTokenizer.Tokenize(string.Empty).Should().BeEmpty();
    }

    [Test]
    public void Tokenize_KeywordsAreCaseInsensitive()
    {
        var text = "select FROM Where";

        var tokens = Classify(text);

        tokens.Where(t => t.Kind == SqlTokenKind.Keyword).Select(t => t.Value)
            .Should().Equal("select", "FROM", "Where");
    }

    private static List<(SqlTokenKind Kind, string Value)> Classify(string text) =>
        SqlTokenizer.Tokenize(text)
            .Select(t => (t.Kind, text.Substring(t.Start, t.Length)))
            .ToList();
}
