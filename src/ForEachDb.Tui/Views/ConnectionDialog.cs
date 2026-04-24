using Dapper;
using ForEachDb.Tui.Infrastructure;
using ForEachDb.Tui.Models;
using ForEachDbQueries;
using ForEachDbQueries.DapperExtensions;
using Npgsql;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public sealed class ConnectionDialog : Dialog
{
    public Recipe? LoadedRecipe { get; private set; }

    private const int FieldX = 22;
    private const int FieldWidth = 32;

    private readonly TextField _host;
    private readonly TextField _port;
    private readonly TextField _database;
    private readonly TextField _username;
    private readonly TextField _password;
    private readonly CheckBox _includePostgres;
    private readonly CheckBox _includeTemplate;
    private readonly TextField _ignoreList;
    private readonly Label _status;
    private readonly Button _connect;
    private readonly Button _loadRecipe;
    private readonly Button _cancel;

    private bool _probing;

    public ConnectionResult? Result { get; private set; }

    public ConnectionDialog() : base("Connect to PostgreSQL cluster", 64, 22)
    {
        _host = Field("localhost", 1);
        _port = Field("5432", 2);
        _database = Field("postgres", 3);
        _username = Field("postgres", 4);
        _password = Field(string.Empty, 5, secret: true);

        _includePostgres = new CheckBox("Include postgres database") { X = 2, Y = 7 };
        _includeTemplate = new CheckBox("Include template databases") { X = 2, Y = 8 };

        _ignoreList = Field(string.Empty, 10, width: FieldWidth);

        _status = new Label(string.Empty)
        {
            X = 2,
            Y = 12,
            Width = Dim.Fill() - 2,
            Height = 2,
            ColorScheme = Colors.Error
        };

        Add(
            new Label("Host:") { X = 2, Y = 1 }, _host,
            new Label("Port:") { X = 2, Y = 2 }, _port,
            new Label("Database:") { X = 2, Y = 3 }, _database,
            new Label("Username:") { X = 2, Y = 4 }, _username,
            new Label("Password:") { X = 2, Y = 5 }, _password,
            _includePostgres,
            _includeTemplate,
            new Label("Ignore (comma sep):") { X = 2, Y = 10 }, _ignoreList,
            _status);

        _connect = new Button("Connect", is_default: true);
        _loadRecipe = new Button("Load recipe");
        _cancel = new Button("Cancel");

        _connect.Clicked += OnConnect;
        _cancel.Clicked += () =>
        {
            Result = null;
            Application.RequestStop();
        };
        _loadRecipe.Clicked += OnLoadRecipe;

        AddButton(_connect);
        AddButton(_loadRecipe);
        AddButton(_cancel);
    }

    private TextField Field(string initial, int y, bool secret = false, int width = FieldWidth)
    {
        var field = new TextField(initial)
        {
            X = FieldX,
            Y = y,
            Width = width,
            Secret = secret
        };
        return field;
    }

    private void OnLoadRecipe()
    {
        if (_probing) return;

        var recipe = LoadRecipeDialog.Prompt(new RecipeStore());
        if (recipe is null) return;

        LoadedRecipe = recipe;

        _host.Text = recipe.Connection.Host;
        _port.Text = recipe.Connection.Port.ToString();
        _database.Text = recipe.Connection.Database;
        _username.Text = recipe.Connection.Username;
        _password.Text = string.Empty;
        _includePostgres.Checked = recipe.Connection.IncludePostgresDb;
        _includeTemplate.Checked = recipe.Connection.IncludeTemplateDb;
        _ignoreList.Text = string.Join(", ", recipe.Connection.IgnoreDatabases);

        _password.SetFocus();
        SetStatus($"Loaded \"{recipe.Name}\". Enter password and press Connect.", warning: false);
    }

    private void OnConnect()
    {
        if (_probing) return;

        var settings = Capture();
        if (settings is null) return;

        SetStatus("Connecting…", warning: false);
        SetBusy(true);

        _ = Task.Run(async () =>
        {
            try
            {
                var databases = await ProbeAsync(settings);
                Application.MainLoop.Invoke(() =>
                {
                    if (databases.Count == 0)
                    {
                        SetBusy(false);
                        SetStatus("Connected — but no databases matched the current filters. Adjust and try again.", warning: true);
                        return;
                    }

                    Result = new ConnectionResult(settings, databases, LoadedRecipe);
                    Application.RequestStop();
                });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                Application.MainLoop.Invoke(() =>
                {
                    SetBusy(false);
                    SetStatus($"Error: {message}", warning: true);
                });
            }
        });
    }

    private ConnectionSettings? Capture()
    {
        if (string.IsNullOrWhiteSpace(_host.Text.ToString()))
        {
            SetStatus("Host is required.", warning: true);
            return null;
        }

        if (!int.TryParse(_port.Text.ToString(), out var port) || port is < 1 or > 65535)
        {
            SetStatus("Port must be a number between 1 and 65535.", warning: true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(_username.Text.ToString()))
        {
            SetStatus("Username is required.", warning: true);
            return null;
        }

        var ignore = (_ignoreList.Text?.ToString() ?? string.Empty)
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        return new ConnectionSettings(
            _host.Text.ToString() ?? "localhost",
            port,
            string.IsNullOrWhiteSpace(_database.Text.ToString()) ? "postgres" : _database.Text.ToString()!,
            _username.Text.ToString() ?? "postgres",
            _password.Text.ToString() ?? string.Empty,
            _includePostgres.Checked,
            _includeTemplate.Checked,
            ignore);
    }

    private static async Task<IReadOnlyList<string>> ProbeAsync(ConnectionSettings settings)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            Username = settings.Username,
            Password = settings.Password
        };

        var finder = new DatabaseFinder();
        if (!settings.IncludePostgresDb) finder.IgnorePostgresDb();
        if (!settings.IncludeTemplateDb) finder.IgnoreTemplateDb();
        if (settings.IgnoreDatabases.Count > 0) finder.IgnoreDatabases(settings.IgnoreDatabases);
        finder.OrderByName();

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return (await connection.QueryAsync<string>(finder)).ToList();
    }

    private void SetStatus(string message, bool warning)
    {
        _status.Text = message;
        _status.ColorScheme = warning ? Colors.Error : Colors.Dialog;
        _status.SetNeedsDisplay();
    }

    private void SetBusy(bool busy)
    {
        _probing = busy;
        _connect.Enabled = !busy;
        _loadRecipe.Enabled = false;
        _cancel.Enabled = !busy;
        foreach (var view in new View[] { _host, _port, _database, _username, _password, _includePostgres, _includeTemplate, _ignoreList })
        {
            view.Enabled = !busy;
        }
    }
}

public sealed record ConnectionResult(
    ConnectionSettings Settings,
    IReadOnlyList<string> Databases,
    Recipe? Recipe = null);
