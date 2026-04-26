using AwesomeAssertions;
using ForEachDb.Desktop.ViewModels;
using NUnit.Framework;

namespace ForEachDb.Desktop.Tests;

public class DatabaseVisibilityTests
{
    [TestCase("",      true)]   // empty filter shows everything
    [TestCase("   ",   true)]   // whitespace-only collapses to empty
    [TestCase("app",   true)]   // substring contains
    [TestCase("APP",   true)]   // case-insensitive substring
    [TestCase("xyz",   false)]
    public void Substring_match(string search, bool expected)
    {
        DatabaseVisibility.Matches("my_app_prod", isTemplate: false, search: search, showTemplates: false)
            .Should().Be(expected);
    }

    [TestCase("b*",        "billing",   true)]   // glob: starts with b
    [TestCase("b*",        "abc",       false)]  // would have been a regex bug
    [TestCase("*log*",     "eventlog",  true)]
    [TestCase("*log*",     "logsink",   true)]
    [TestCase("*log*",     "audit",     false)]
    [TestCase("app_?",     "app_a",     true)]   // ? = single char
    [TestCase("app_?",     "app_ab",    false)]
    [TestCase("APP_*",     "app_prod",  true)]   // case-insensitive
    public void Glob_match(string search, string name, bool expected)
    {
        DatabaseVisibility.Matches(name, isTemplate: false, search: search, showTemplates: false)
            .Should().Be(expected);
    }

    [Test]
    public void Template_databases_are_hidden_unless_show_templates_is_on()
    {
        DatabaseVisibility.Matches("template1", isTemplate: true, search: "", showTemplates: false)
            .Should().BeFalse();

        DatabaseVisibility.Matches("template1", isTemplate: true, search: "", showTemplates: true)
            .Should().BeTrue();
    }

    [Test]
    public void Template_filter_combines_with_search()
    {
        DatabaseVisibility.Matches("template1", isTemplate: true, search: "temp*", showTemplates: false)
            .Should().BeFalse("template hidden even when matching the search");

        DatabaseVisibility.Matches("template1", isTemplate: true, search: "temp*", showTemplates: true)
            .Should().BeTrue();

        DatabaseVisibility.Matches("template1", isTemplate: true, search: "xyz",   showTemplates: true)
            .Should().BeFalse("template visible but search doesn't match");
    }
}
