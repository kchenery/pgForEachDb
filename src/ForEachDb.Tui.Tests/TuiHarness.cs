using ForEachDb.Tui.Models;
using ForEachDb.Tui.Theme;
using ForEachDb.Tui.Views;
using Terminal.Gui;

namespace ForEachDb.Tui.Tests;

/// <summary>
/// Initializes Terminal.Gui with the in-memory <see cref="FakeDriver" /> so tests can
/// construct views and synthesise key events without a real terminal. Dispose to
/// tear the application state down between tests.
/// </summary>
public sealed class TuiHarness : IDisposable
{
    public static TuiHarness Start()
    {
        Application.Init(new FakeDriver());
        Themes.Apply(ThemeKind.SofterDark);
        return new TuiHarness();
    }

    public void Dispose()
    {
        Application.Shutdown();
    }

    public static ConnectionResult FakeConnection(params string[] databases) =>
        new(
            new ConnectionSettings(
                Host: "localhost",
                Port: 5432,
                Database: "postgres",
                Username: "tester",
                Password: "pw",
                IncludePostgresDb: false,
                IncludeTemplateDb: false,
                IgnoreDatabases: Array.Empty<string>()),
            databases.Length == 0 ? new[] { "alpha", "beta", "gamma" } : databases,
            Recipe: null);

    public static KeyEvent Key(Key key) => new(key, new KeyModifiers());
}
