namespace ForEachDb.Tui.Infrastructure;

public enum SqlTokenKind
{
    Whitespace,
    Keyword,
    Identifier,
    Number,
    String,
    Comment,
    Punctuation
}

public readonly record struct SqlToken(int Start, int Length, SqlTokenKind Kind);

/// <summary>
/// Lightweight Postgres-flavoured SQL tokenizer. Handles single-quoted strings,
/// dollar-quoted strings, line + block comments, numeric literals, keywords,
/// identifiers, and punctuation. Intended for syntax coloring — not a parser.
/// </summary>
public static class SqlTokenizer
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE",
        "CREATE", "TABLE", "DROP", "ALTER", "INDEX", "VIEW", "DATABASE", "SCHEMA", "FUNCTION",
        "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "FULL", "CROSS", "ON", "USING",
        "AS", "AND", "OR", "NOT", "NULL", "IS", "IN", "LIKE", "ILIKE", "BETWEEN", "EXISTS",
        "ORDER", "BY", "GROUP", "HAVING", "LIMIT", "OFFSET", "FETCH", "FIRST", "ROWS", "ONLY",
        "UNION", "INTERSECT", "EXCEPT", "ALL", "DISTINCT", "WITH", "RECURSIVE", "RETURNING",
        "ANALYZE", "VACUUM", "REINDEX", "CLUSTER", "EXPLAIN", "COPY",
        "BEGIN", "COMMIT", "ROLLBACK", "SAVEPOINT", "TRANSACTION",
        "DO", "IF", "THEN", "ELSE", "ELSIF", "END", "CASE", "WHEN",
        "LANGUAGE", "RAISE", "NOTICE", "WARNING", "EXCEPTION",
        "TRUE", "FALSE", "UNKNOWN",
        "GRANT", "REVOKE", "PRIVILEGES", "TO", "PUBLIC"
    };

    public static IEnumerable<SqlToken> Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        var i = 0;
        while (i < text.Length)
        {
            var ch = text[i];

            if (char.IsWhiteSpace(ch))
            {
                var start = i;
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                yield return new SqlToken(start, i - start, SqlTokenKind.Whitespace);
                continue;
            }

            if (ch == '-' && i + 1 < text.Length && text[i + 1] == '-')
            {
                var start = i;
                while (i < text.Length && text[i] != '\n') i++;
                yield return new SqlToken(start, i - start, SqlTokenKind.Comment);
                continue;
            }

            if (ch == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var start = i;
                i += 2;
                while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++;
                i = i + 1 < text.Length ? i + 2 : text.Length;
                yield return new SqlToken(start, i - start, SqlTokenKind.Comment);
                continue;
            }

            if (ch == '$')
            {
                var tagEnd = i + 1;
                while (tagEnd < text.Length && (char.IsLetterOrDigit(text[tagEnd]) || text[tagEnd] == '_')) tagEnd++;
                if (tagEnd < text.Length && text[tagEnd] == '$')
                {
                    var start = i;
                    var tag = text.AsSpan(i, tagEnd - i + 1).ToString();
                    i = tagEnd + 1;
                    var end = text.IndexOf(tag, i, StringComparison.Ordinal);
                    i = end >= 0 ? end + tag.Length : text.Length;
                    yield return new SqlToken(start, i - start, SqlTokenKind.String);
                    continue;
                }
            }

            if (ch == '\'')
            {
                var start = i;
                i++;
                while (i < text.Length)
                {
                    if (text[i] == '\'' && i + 1 < text.Length && text[i + 1] == '\'') { i += 2; continue; }
                    if (text[i] == '\'') { i++; break; }
                    i++;
                }
                yield return new SqlToken(start, i - start, SqlTokenKind.String);
                continue;
            }

            if (char.IsDigit(ch))
            {
                var start = i;
                var sawDot = false;
                while (i < text.Length)
                {
                    if (char.IsDigit(text[i])) { i++; continue; }
                    if (text[i] == '.' && !sawDot) { sawDot = true; i++; continue; }
                    break;
                }
                yield return new SqlToken(start, i - start, SqlTokenKind.Number);
                continue;
            }

            if (char.IsLetter(ch) || ch == '_')
            {
                var start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i++;
                var word = text[start..i];
                yield return new SqlToken(start, i - start,
                    Keywords.Contains(word) ? SqlTokenKind.Keyword : SqlTokenKind.Identifier);
                continue;
            }

            yield return new SqlToken(i, 1, SqlTokenKind.Punctuation);
            i++;
        }
    }
}
