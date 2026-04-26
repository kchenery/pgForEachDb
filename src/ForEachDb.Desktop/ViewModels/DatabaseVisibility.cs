using System.Text.RegularExpressions;

namespace ForEachDb.Desktop.ViewModels;

/// <summary>
/// Decides whether a database should be visible in the sidebar list given the user's filter
/// inputs. Pure function so it can be unit-tested without a UI.
/// </summary>
public static class DatabaseVisibility
{
    public static bool Matches(string name, bool isTemplate, string? search, bool showTemplates)
    {
        if (isTemplate && !showTemplates) return false;

        var trimmed = search?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) return true;

        if (trimmed.Contains('*') || trimmed.Contains('?'))
        {
            var pattern = "^" + Regex.Escape(trimmed).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return name.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
    }
}
