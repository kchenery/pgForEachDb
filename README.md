# pgForEachDb

Run a SQL statement across multiple PostgreSQL databases on a server in parallel. Useful for maintenance commands like `ANALYZE`, `VACUUM`, or any query you need to execute against every database.

Features a live progress display showing which databases are running, which have completed, and which failed.

## Interactive mode

Launching with no arguments (or `--tui`) opens a Spectre.Console interactive wizard: connect, pick databases, enter SQL, watch a live two-panel display as it runs, see a results summary, then a menu for what's next.

```bash
./ForEachDb          # launches the interactive flow
./ForEachDb --tui    # same thing
```

### Flow

1. **Connect** — prompts for host/port/user/password/filters, or pick a saved recipe.
2. **Select databases** — multi-select prompt (space to toggle, enter to confirm).
3. **Enter SQL** — single-line terminated by `;`, or multi-line terminated by `;;` on its own line. Empty input returns to the menu.
4. **Live run** — two-panel display: database states on the left with animated spinners, log stream (NOTICE / INFO / ERROR) on the right. `Ctrl+C` cancels.
5. **Results** — if the query returned rows, a table summary prints (capped at 50 rows for display; CSV export writes everything).
6. **Menu** — re-run same query · new query · change selection · change threads · save recipe · export CSV · change cluster · quit.

### Recipes

A *recipe* bundles a connection (host / port / user / db / filters), a database selection, a query, and a thread count under a name. Recipes never store passwords — you always re-enter on load.

- **Save** — choose "Save current as recipe" from the menu after any run.
- **Load** — at the connection step, choose "Load recipe" instead of entering details.

Recipes live at `$XDG_CONFIG_HOME/pgForEachDb/recipes.json` (Unix, defaulting to `~/.config/pgForEachDb/recipes.json`) or `%APPDATA%\pgForEachDb\recipes.json` (Windows).

## CLI usage

```
./ForEachDb --help

  -q, --query              Query to run against each database
  -i, --interactive        [Deprecated] Old minimal loop. Launch with no args for the richer wizard.
  -h, --host               (Default: localhost) Hostname to connect to
  -d, --database           (Default: postgres) Database to connect to
  -u, --username           (Default: postgres) Username for the connection
  -p, --password           Password for the connection
  -t, --threads            (Default: 4) Number of threads to run
  --port                   (Default: 5432) Port for the connection
  --ignore                 List of databases that should be ignored. E.g: --ignore foo bar baz
  --include-postgres-db    Flag to include the postgres database
  --include-template-db    Flag to include template databases
  --help                   Display this help screen
  --version                Display version information
```

## Examples

### Run ANALYZE across all databases

```bash
./ForEachDb -q "ANALYZE;" -h localhost -u postgres
```

### Run VACUUM on all databases with 8 parallel threads

```bash
./ForEachDb -q "VACUUM;" -h localhost -u postgres -t 8
```

### Ignore specific databases

```bash
./ForEachDb -q "ANALYZE;" -h localhost -u postgres --ignore staging_old test_db
```

### Interactive mode

Launch interactive mode to pick which databases to target and enter queries on the fly:

```bash
./ForEachDb -i -h localhost -u postgres
```

In interactive mode you can:
- Select databases from a multi-select list (space to toggle, enter to confirm)
- Enter a SQL query at the `SQL>` prompt
- Type `\r` to go back and reselect databases (previous selections are retained)
- Press enter with no input to exit

### Progress display

Queries run in parallel with a live progress display:

```
✔ customer_db
✔ inventory_db
✘ broken_db - 42601: syntax error at or near "anaylze" POSITION: 1
────────────────────────────────────────────────────────
⡇ orders_db
⠃ analytics_db

3 / 5 databases completed
```

- Green checkmark for successful completions
- Red cross for failures with the error message
- Animated spinner for databases currently running

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) and require Docker to be running.
