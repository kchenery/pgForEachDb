# pgForEachDb

Run a SQL statement across multiple PostgreSQL databases on a server in parallel. Useful for maintenance commands like `ANALYZE`, `VACUUM`, or any query you need to execute against every database.

Two ways to drive it:

- **Desktop app** — an Avalonia GUI for ad-hoc work and exploration.
- **CLI** — for scripts and pipelines.

## Desktop app

Launching with no arguments opens the Avalonia desktop app.

```bash
./ForEachDb        # opens the desktop app
./ForEachDb --tui  # same thing
```

The app has two screens:

1. **Connection** — host / port / user / password, plus the list of saved *recipes*. Double-click a recipe to populate the form; you still re-enter the password.
2. **Workspace** — three panes (Query, Results, Log) and a sidebar with the database list:
   - **Database list** — every non-template database is selected by default. A search box filters by glob (`b*`, `*_log`, `app_?`) or substring; toggle **Templates** to include `template0` / `template1` / etc. Selection survives filter changes; **All** / **None** apply to whatever is visible.
   - **Query** — AvaloniaEdit with PostgreSQL syntax highlighting. `Ctrl+Enter` runs, `F2` cycles panes.
   - **Results** — a sortable, filterable grid (filter by source database). **Export CSV** dumps the active filter.
   - **Log** — `NOTICE` / `INFO` / `WARNING` / `ERROR` rows from each database, colour-coded by level, also filterable by database. **Save log…** writes a tab-separated file.
   - **Save recipe** — bundles current connection (no password), database selection, query, and thread count under a name. Save into the user config or to a chosen file.

Recipes live at `$XDG_CONFIG_HOME/pgForEachDb/recipes.json` (Unix; defaults to `~/.config/pgForEachDb/recipes.json`) or `%APPDATA%\pgForEachDb\recipes.json` (Windows).

## CLI

For scripted use, pass `-q` (or `--interactive` for a Spectre.Console prompt loop in the terminal):

```
./ForEachDb --help

  -q, --query              Query to run against each database
  -i, --interactive        Spectre.Console prompt loop in the terminal
  -h, --host               (Default: localhost) Hostname to connect to
  -d, --database           (Default: postgres) Maintenance database to connect to
  -u, --username           (Default: postgres) Username for the connection
  -p, --password           Password for the connection (prompted if omitted)
  -t, --threads            (Default: 4) Number of parallel threads
      --port               (Default: 5432) Port for the connection
      --ignore             Databases to skip. e.g. --ignore foo bar baz
      --include-postgres-db    Include the postgres database (off by default)
      --include-template-db    Include template databases (off by default)
      --help               Display this help screen
      --version            Display version information
```

### Examples

```bash
# Run ANALYZE across every database
./ForEachDb -q "ANALYZE;" -h localhost -u postgres

# VACUUM with 8 parallel threads
./ForEachDb -q "VACUUM;" -h localhost -u postgres -t 8

# Skip a couple of databases
./ForEachDb -q "ANALYZE;" -h localhost -u postgres --ignore staging_old test_db

# Interactive prompt loop in the terminal
./ForEachDb -i -h localhost -u postgres
```

In interactive (`-i`) mode you pick databases from a multi-select prompt and enter SQL at a `SQL>` prompt; type `\r` to reselect databases, empty input exits.

### Progress output (CLI)

```
✔ customer_db
✔ inventory_db
✘ broken_db - 42601: syntax error at or near "anaylze" POSITION: 1
────────────────────────────────────────────────────────
⡇ orders_db
⠃ analytics_db

3 / 5 databases completed
```

Green check = success, red cross = failure with error, animated spinner = running.

## Building and testing

```bash
dotnet build
dotnet test
```

Integration tests under `ForEachDbQueries.Tests` use [Testcontainers](https://dotnet.testcontainers.org/) and require Docker. The `ForEachDb.Desktop.Tests` project is pure unit tests with no Docker dependency.

## Project layout

| Folder    | Project                  | Purpose                                          |
|-----------|--------------------------|--------------------------------------------------|
| `App`     | `ForEachDb.Desktop`      | Avalonia 12 GUI                                  |
| `Library` | `ForEachDbQueries`       | Domain library: runner, recipes, exporters       |
| `CLI`     | `ForEachDb`              | Command-line / Spectre interactive entry point   |
| `Tests`   | `ForEachDb.Desktop.Tests`| Unit tests for the desktop view-models           |
| `Tests`   | `ForEachDbQueries.Tests` | Integration tests for the domain library         |

See [`docs/architecture-review.md`](docs/architecture-review.md) for the maintainability log and current open items.
