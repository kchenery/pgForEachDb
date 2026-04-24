# pgForEachDb

Run a SQL statement across multiple PostgreSQL databases on a server in parallel. Useful for maintenance commands like `ANALYZE`, `VACUUM`, or any query you need to execute against every database.

Features a live progress display showing which databases are running, which have completed, and which failed.

## TUI

Launching with no arguments (or `--tui`) opens a full Terminal.Gui interface with database selection, query editor, live per-database status, a scrollable log pane, results grid, CSV export, and saved "recipes".

```bash
./ForEachDb          # launches the TUI
./ForEachDb --tui    # same thing
```

### Layout

```
┌ File │ Run │ View │ Recipes │ Help ─────────────────────────────────┐
│ ┌─ Databases ──────┐ ┌─ Log (all) ──────────────────────────────┐   │
│ │ [x] ✔ analytics  │ │ 14:02:11 analytics  INFO   Query started │   │
│ │ [x] ⡇ orders     │ │ 14:02:12 analytics  INFO   Completed…    │   │
│ │ [x] ○ billing    │ │ 14:02:11 orders     NOTICE autovacuum…   │   │
│ └──────────────────┘ └──────────────────────────────────────────┘   │
│ ┌─ SQL — Ctrl+Enter or F5 to run ·  Ctrl+Up/Down history ─────────┐ │
│ │ ANALYZE;                                                        │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│ 12/12 selected  threads 8  RUNNING  2 done, 0 failed  …  F5 run …  │
└────────────────────────────────────────────────────────────────────┘
```

### Keybindings

| Keys | Action |
| --- | --- |
| `F5` / `Ctrl+Enter` | Run query against selected databases |
| `F6` | Cancel the active run |
| `Esc` / `Ctrl+Q` | Quit (`Esc` during a run cancels first) |
| `Space` | Toggle the highlighted database |
| `Ctrl+A` / `Ctrl+N` | Select all / none |
| `Ctrl+T` | Change thread count (1–64) |
| `Ctrl+L` | Cycle log filter: all → selected DB → failed only |
| `Ctrl+R` | Open the results grid (after a run that returned rows) |
| `Ctrl+E` | Export results as CSV (inside the results grid) |
| `Ctrl+S` | Save current connection + selection + query as a recipe |
| `Ctrl+Up` / `Ctrl+Down` | Previous / next query from session history |
| `Enter` on a failed row | Show the full error message |
| `F1` | Keybindings help |

### Results and CSV export

When a `SELECT` finishes, press `Ctrl+R` to open a grid showing every row with a `database` column prepended. If databases return different column sets, the grid shows the union and fills missing cells with blanks. `Ctrl+E` inside the grid streams a full CSV to a chosen file — the grid preview caps at 10,000 rows but the export always contains everything.

### Recipes

A *recipe* bundles a connection (host / port / user / db / filters), a database selection, a query, and a thread count into a named save. Recipes never store passwords.

- **Save** — `Ctrl+S` from the main window
- **Load** — "Load recipe" button in the connection dialog at launch; after loading, enter the password and press Connect

Recipes live at `$XDG_CONFIG_HOME/pgForEachDb/recipes.json` (Unix, defaulting to `~/.config/pgForEachDb/recipes.json`) or `%APPDATA%\pgForEachDb\recipes.json` (Windows).

## CLI usage

```
./ForEachDb --help

  -q, --query              Query to run against each database
  -i, --interactive        [Deprecated] Legacy Spectre interactive mode. Use the TUI instead (no args).
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
