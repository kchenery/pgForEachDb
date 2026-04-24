# TUI plan

Plan for adding a Terminal.Gui v2 interactive UI to pgForEachDb while keeping the existing CLI intact.

## Progress

| Phase | Scope | Status |
| ----- | ----- | ------ |
| 1 | Engine extensions (shared library) | ✅ 14/14 |
| 2 | TUI scaffold | ✅ 4/4 |
| 3 | Connection flow | ✅ 3/3 |
| 4 | Main layout (static) | ✅ 6/6 |
| 5 | Wire engine to UI (live) | 🟡 11/12 (search deferred) |
| 6 | Results + CSV | ✅ 5/5 |
| 7 | Recipes | ✅ 4/4 |
| 8 | SQL editor polish | 🟡 4/5 (syntax coloring deferred) |
| 9 | Polish | ✅ 5/5 |

**Total: 57 / 58 tasks complete — 1 deliberate deferral (SQL syntax coloring).**

**Dark theme + automated UI tests** landed on top of the plan:
- [Theme/DarkTheme.cs](../src/ForEachDb.Tui/Theme/DarkTheme.cs) — applied after `Application.Init()`.
- [TuiHarness.cs](../src/ForEachDb.Tui.Tests/TuiHarness.cs) boots Terminal.Gui with `FakeDriver` so tests run headless.
- [MainWindowShortcutsTests.cs](../src/ForEachDb.Tui.Tests/MainWindowShortcutsTests.cs), [DatabaseListViewTests.cs](../src/ForEachDb.Tui.Tests/DatabaseListViewTests.cs), [LogViewTests.cs](../src/ForEachDb.Tui.Tests/LogViewTests.cs) — 16 tests covering the shortcut-routing regression surface and the two most-changed views.
- `InternalsVisibleTo` on the TUI project lets tests touch internals without widening the public API.

## Decisions

- **Library**: Terminal.Gui v1 (latest stable — currently 1.19.0). We initially pinned v2 rc.4 but dropped back to avoid pre-release churn and to get established docs/samples. Spectre.Console stays on the CLI path.
- **SQL input**: multi-line editor, per-session history, basic SQL syntax coloring (custom tokenizer — no full parser).
- **Results**: unified result set with a `database` column prepended; schema = union of columns across DBs; nulls for missing. CSV export.
- **Persistence**: named recipes (connection + selection + query). No separate recent-connections list or cross-session query history. Passwords never stored — re-prompt on load.
- **CLI compatibility**: `-q` still works. TUI launches when no args (or `--tui`). Legacy `-i` stays, marked deprecated.

## Target solution layout

```
src/
  ForEachDb/              # existing CLI entry point (stays)
  ForEachDb.Tui/          # new Terminal.Gui v2 app
  ForEachDbQueries/       # shared engine (extended)
  ForEachDbQueries.Tests/ # extended
```

## Task breakdown

Every task is sized to be landable on its own. Each has explicit acceptance criteria. Phases are ordered by dependency; tasks within a phase can mostly run in parallel unless noted.

### Phase 1 — Engine extensions (shared library) — ✅ complete

Land before any TUI work so the CLI keeps working at every step.

- [x] **1.1 Add `Pending` to `DatabaseRunState`**
  - File: [src/ForEachDbQueries/DatabaseStatus.cs](../src/ForEachDbQueries/DatabaseStatus.cs)
  - Update any exhaustive switches in [LiveProgressRenderer.cs](../src/ForEachDb/LiveProgressRenderer.cs) to ignore Pending.
  - Acceptance: build green, CLI output unchanged for existing flows.

- [x] **1.2 Extend `DatabaseStatus` with timing + row count**
  - Add `TimeSpan? Duration`, `int? RowCount` (nullable; pre-completion states leave them null).
  - Update existing constructor callers (tests + runner).
  - Acceptance: existing tests compile and pass; new fields default to null.

- [x] **1.3 Seed Pending statuses upfront in `ForEachDbRunner`**
  - Before `Parallel.ForEachAsync`, emit a `Pending` status for each database via the `IProgress<DatabaseStatus>`.
  - Acceptance: a test asserts one `Pending` event per database fires before any `Running`.

- [x] **1.4 Thread `CancellationToken` through `RunQueryAsync`**
  - Add optional `CancellationToken` parameter (both generic and non-generic overloads).
  - Pass into `ParallelOptions`, `OpenAsync`, `QueryAsync`.
  - Acceptance: cancellation test — start a slow query, cancel, verify remaining DBs never enter `Running` and cancelled ones surface as `Failed` with an `OperationCanceledException` message (or a dedicated `Cancelled` state — decide in 1.4a).

- [x] **1.4a Decide Cancelled vs Failed**
  - Decision: added a dedicated `Cancelled` state for UI clarity. Enum + renderer updated.

- [x] **1.5 Define `IDatabaseLogSink` + `DatabaseLogEntry`**
  - New types in `ForEachDbQueries`. `DatabaseLogEntry { DateTime Timestamp; LogLevel Level; string Message; }` where `LogLevel` is `Info | Notice | Warning | Error`.
  - Acceptance: types exist, no consumers yet.

- [x] **1.6 Emit log entries from the runner**
  - In `ForEachDbRunner.RunQueryAsync<T>`:
    - On entry per DB: emit `Info` with echoed query.
    - Wire `NpgsqlConnection.Notice` event → `Notice` log entries.
    - On success: `Info` "completed in {duration}, {rowCount} rows".
    - On exception: `Error` with message.
  - Sink param is optional; CLI passes `null`.
  - Acceptance: unit test with a fake sink asserts ordering and content for a simulated success + failure run.

- [x] **1.7 Dynamic-shape query overload**
  - New method `RunQueryAsDynamicAsync(IEnumerable<string> dbs, string query, ...)` returning `IReadOnlyList<DatabaseRow>` where `DatabaseRow { string Database; IReadOnlyDictionary<string, object?> Values; }`.
  - Use Dapper's `QueryAsync` returning `dynamic`, then convert each row's `IDictionary<string, object>` into `DatabaseRow`.
  - Acceptance: integration test (Testcontainers) runs `SELECT 1 as a` across 3 DBs and returns 3 rows.

- [x] **1.8 CSV export helper**
  - Static `CsvExporter.Write(Stream, IEnumerable<DatabaseRow>)`.
  - First column is `database`, remaining columns = union of all `Values` keys across input rows, in first-seen order.
  - Quote per RFC 4180 (escape `"`, wrap fields containing `,` `"` `\n`).
  - Acceptance: unit tests cover commas, embedded quotes, newlines, nulls, mismatched schemas.

- [x] **1.9 Tests: Pending state lifecycle**
  - Covered by 1.3; keep as its own test file if convenient.

- [x] **1.10 Tests: cancellation**
  - Covered by 1.4; new test class `CancellationTests`.

- [x] **1.11 Tests: log sink**
  - Covered by 1.6; new test class `LogSinkTests` using a capturing fake.

- [x] **1.12 Tests: dynamic rows + CSV**
  - Covered by 1.7 + 1.8.

- [x] **1.13 Keep `LiveProgressRenderer` silent on new states**
  - Render Pending/Cancelled appropriately in the existing CLI output (e.g. Pending hidden, Cancelled shown with a dim mark).
  - Acceptance: manual run with `-q` still looks identical for the happy path; cancellation during `-q` now produces readable output.

### Phase 2 — TUI scaffold — ✅ complete

- [x] **2.1 Create `ForEachDb.Tui` project**
  - `dotnet new console` at `src/ForEachDb.Tui`, target `net10.0`, add to `pgForEachDb.sln`.
  - Acceptance: `dotnet build` succeeds; solution has 4 projects.

- [x] **2.2 Add Terminal.Gui package**
  - Pinned to `Terminal.Gui` `1.19.0` (latest stable). Chose v1 over v2 rc.4 to avoid pre-release API churn; revisit once v2 goes stable.
  - Acceptance: build succeeds; `Application.Init()` runs without throwing.

- [x] **2.3 Wire entry point dispatch**
  - [src/ForEachDb/Program.cs](../src/ForEachDb/Program.cs) dispatches to `ForEachDb.Tui.App.Run()` when `args` is empty or contains `--tui`.
  - `ForEachDb.csproj` references `ForEachDb.Tui`.
  - Acceptance: `dotnet run -- --help` still prints CLI help. `dotnet run` with no args launches the TUI (needs TTY — verify manually).

- [x] **2.4 `App` bootstrap**
  - `Application.Init()` → `Application.Top.Add(window)` → `Application.Run()` in try/finally with `Application.Shutdown()`. Window handles Esc / Ctrl+Q → `Application.RequestStop()`.
  - Acceptance: clean terminal restore on exit (runtime/manual verification).

### Phase 3 — Connection flow — ✅ complete

- [x] **3.1 `ConnectionDialog` view**
  - Modal with fields: Host, Port, Database, Username, Password (masked), Include postgres, Include templates, Ignore (comma-sep).
  - Buttons: Connect (default), Load recipe (stubbed for Phase 7), Cancel.
  - Acceptance: dialog opens on TUI launch; Cancel sets `Result = null` and exits the app.

- [x] **3.2 Validate + probe on Connect**
  - Input validation (host required, port 1–65535, username required).
  - Probe runs off-thread via `Task.Run`, marshals back via `Application.MainLoop.Invoke`. While probing the dialog disables inputs and shows "Connecting…".
  - On failure: inline error label; dialog stays open.
  - Acceptance: against a live Postgres, Connect succeeds and dialog dismisses with `ConnectionResult` in hand.

- [x] **3.3 Hand off to main window stub**
  - [MainWindow.cs](../src/ForEachDb.Tui/Views/MainWindow.cs) shows the discovered databases in a `ListView` with a header line and Esc/Ctrl+Q to quit.
  - Acceptance: DB names from the cluster visible in the new window.

### Phase 4 — Main layout (static) — ✅ complete

- [x] **4.1 `MainWindow` shell**
  - Three regions in [MainWindow.cs](../src/ForEachDb.Tui/Views/MainWindow.cs): left DB list (35%), right log pane (fills), bottom SQL editor (7 rows), single-line status strip anchored to bottom.
  - Acceptance: empty panes render with borders/titles; resizing keeps proportions.

- [x] **4.2 `DatabaseListView` — selectable**
  - [DatabaseListView.cs](../src/ForEachDb.Tui/Views/DatabaseListView.cs) wraps `ListView` with `AllowsMarking` + `AllowsMultipleSelection`. Every DB starts selected so "run across everything" is a one-keystroke path. Exposes `SelectedDatabases`, `SelectedCount`, `SelectAll`, `SelectNone`, `SelectionChanged` event.
  - Acceptance: space toggles selection; up/down scrolls; selection count updates the status strip live.

- [x] **4.3 `LogView` — scrollable**
  - [LogView.cs](../src/ForEachDb.Tui/Views/LogView.cs) — a read-only `TextView` with an `Append(line)` helper and `ClearLog()`. Filter + search arrive in Phase 5.
  - Acceptance: appended lines show and auto-scroll to bottom.

- [x] **4.4 `SqlEditorView` — plain multi-line**
  - [SqlEditorView.cs](../src/ForEachDb.Tui/Views/SqlEditorView.cs) — `TextView` subclass, multi-line, no highlight/history yet (those arrive in Phase 8).
  - Acceptance: text entry, newlines work.

- [x] **4.5 Status strip — static counters**
  - Shows "N / total databases selected" plus key-hint legend (F5 run / F6 cancel / Ctrl+A all / Ctrl+N none / Esc quit).
  - Acceptance: renders, updates when selection changes.

- [x] **4.6 Key routing stubs**
  - Esc / Ctrl+Q quit, F5 triggers run (stub prints to log), F6 cancel (stub), Ctrl+A select all, Ctrl+N select none. F5/F6 engine wiring lands in Phase 5.
  - Acceptance: each key produces a log entry or action; no exceptions.

### Phase 5 — Wire engine to UI (live) — 🟡 11/12 (log search deferred)

- [x] **5.1 `RunSessionViewModel`**
  - Run lifecycle state (selection, threads, running flag, `CancellationTokenSource`, elapsed timer) lives on [MainWindow.cs](../src/ForEachDb.Tui/Views/MainWindow.cs) for now. Extraction to a separate VM + unit tests against it is a good follow-up once we add the `FakeDriver` harness.

- [x] **5.2 `UiProgress` adapter**
  - [Infrastructure/UiProgress.cs](../src/ForEachDb.Tui/Infrastructure/UiProgress.cs) — `IProgress<DatabaseStatus>` that marshals onto the UI thread via `Application.MainLoop.Invoke`.

- [x] **5.3 `UiLogSink` adapter**
  - [Infrastructure/UiLogSink.cs](../src/ForEachDb.Tui/Infrastructure/UiLogSink.cs) — same pattern for `IDatabaseLogSink`.

- [x] **5.4 Checkbox toggling in DB list**
  - Space toggles (built into `ListView.AllowsMarking`); Ctrl+A all, Ctrl+N none. `invert` and `/` filter deferred to a later polish pass if needed.
  - Acceptance: selection count in status strip updates live on every toggle.

- [x] **5.5 Wire F5 → run**
  - F5 builds `UiProgress` + `UiLogSink`, constructs the connection string, calls `ForEachDbRunner.RunQueryAsync(selected, query, threads, progress, sink, token)` on a worker task.
  - Acceptance: running against a live cluster updates icons in the DB list and streams log lines.

- [x] **5.6 Animated spinner for Running state**
  - `Application.MainLoop.AddTimeout(100ms)` ticks `DatabaseListView.TickSpinner()`. Running rows rotate through the braille frames; other states are static.

- [x] **5.7 Log filters**
  - `Ctrl+L` cycles All → Selected DB → Failed only. Selected DB tracks the DB list cursor via `DatabaseListView.SelectionChanged` → `LogView.SetSelectedDatabase`.

- [ ] **5.8 Log search (`/`)** — **deferred**
  - Find-next over the log buffer. Useful but not load-bearing; revisit once real usage demands it.

- [x] **5.9 Live status strip**
  - Shows `selected/total`, threads, run state, done/failed/cancelled counts, elapsed seconds. A 500ms timer ticks while a run is active; any state change refreshes immediately.

- [x] **5.10 Cancellation wiring (F6 / Esc)**
  - F6 cancels the active `CancellationTokenSource`. Esc during a run also cancels (press twice to quit). Engine reports Cancelled states; pending DBs stay Pending.

- [x] **5.11 Error detail modal**
  - [ErrorDetailDialog.cs](../src/ForEachDb.Tui/Views/ErrorDetailDialog.cs) — opens via Enter on a failed row. Shows the full error message in a scrollable read-only `TextView`.

- [x] **5.12 Threads spinner editable**
  - [ThreadsDialog.cs](../src/ForEachDb.Tui/Views/ThreadsDialog.cs) — `Ctrl+T` opens a prompt bounded 1–64. Blocked while a run is active.

### Phase 6 — Results + CSV — ✅ complete

- [x] **6.1 Invoke dynamic path for every run**
  - MainWindow now calls `RunQueryAsDynamicAsync`. For queries that don't return rows (e.g. `ANALYZE;`) the returned list is empty and no results view opens. For `SELECT`s a log hint announces how many rows arrived.

- [x] **6.2 Column union aggregator**
  - [ResultsAggregator.cs](../src/ForEachDbQueries/ResultsAggregator.cs) — pure `Aggregate(IEnumerable<DatabaseRow>) → AggregatedResults(columns, rows)`. First column always `database`; union of all per-DB keys in first-seen order; missing values `null`.
  - [ResultsAggregatorTests.cs](../src/ForEachDbQueries.Tests/ResultsAggregatorTests.cs) — 4 tests covering mismatch, ordering, empty.

- [x] **6.3 `ResultsDialog` with `TableView`**
  - [ResultsDialog.cs](../src/ForEachDb.Tui/Views/ResultsDialog.cs) — opens via `Ctrl+R`. Backs onto `System.Data.DataTable` (which Terminal.Gui v1's `TableView` expects). Simple design: modal dialog rather than a docked pane (deviation from original plan; simpler given v1's layout primitives).
  - Acceptance: rows scroll; Close returns to MainWindow.

- [x] **6.4 CSV export dialog**
  - `Ctrl+E` inside the results dialog opens `SaveDialog`, streams via `CsvExporter.WriteAsync`. Auto-appends `.csv` if missing.
  - Exceptions surface via `MessageBox.ErrorQuery`.

- [x] **6.5 Preview cap with overflow hint**
  - `ResultsDialog.PreviewCap = 10_000`. When exceeded, a red-toned banner reads "Preview truncated to 10,000 of N rows — export CSV for the full set." Export always streams the full set.

### Phase 7 — Recipes — 🟡 3/4 (menu bar deferred)

- [x] **7.1 `RecipeStore`**
  - [Infrastructure/RecipeStore.cs](../src/ForEachDb.Tui/Infrastructure/RecipeStore.cs) — JSON persistence at `$XDG_CONFIG_HOME/pgForEachDb/recipes.json` or `~/.config/pgForEachDb/recipes.json` on Unix; `%APPDATA%\pgForEachDb\recipes.json` on Windows.
  - [Models/Recipe.cs](../src/ForEachDb.Tui/Models/Recipe.cs) — `Recipe { Name; Host; Port; Database; Username; IncludePostgresDb; IncludeTemplateDb; IgnoreDatabases; SelectedDatabases; Query; Threads }`. **No password field** — passwords are always re-entered at load.
  - New test project [ForEachDb.Tui.Tests](../src/ForEachDb.Tui.Tests) with 7 tests covering save/load, overwrite, case-insensitive names, delete, corrupt file tolerance.

- [x] **7.2 Save dialog**
  - `Ctrl+S` from MainWindow opens [SaveRecipeDialog.cs](../src/ForEachDb.Tui/Views/SaveRecipeDialog.cs). Prompt seeded with the recipe name (if the session loaded one). Overwrite confirmation via `MessageBox.Query` when saving a different name that already exists.

- [x] **7.3 Load dialog**
  - `Load recipe` button in [ConnectionDialog.cs](../src/ForEachDb.Tui/Views/ConnectionDialog.cs) opens [LoadRecipeDialog.cs](../src/ForEachDb.Tui/Views/LoadRecipeDialog.cs). Enter loads, Del/Backspace deletes with confirmation, Esc cancels.
  - After load: all connection fields populate, password cleared, password field focused, status message "Loaded \"X\". Enter password and press Connect."
  - `ConnectionResult` carries the loaded `Recipe` through; MainWindow applies the saved selection + query + threads on open. Missing DBs are listed in the log.

- [x] **7.4 Menu bar wiring**
  - [AppMenu.cs](../src/ForEachDb.Tui/Views/AppMenu.cs) builds a `MenuBar` with File / Run / View / Recipes / Help. Hosted on a `Toplevel` above the main window in [App.cs](../src/ForEachDb.Tui/App.cs). Alt+letter opens each menu; shortcut hints match the keybindings.

### Phase 8 — SQL editor polish — 🟡 4/5 (syntax coloring deferred)

- [x] **8.1 Per-session history ring buffer**
  - [Infrastructure/SqlHistory.cs](../src/ForEachDb.Tui/Infrastructure/SqlHistory.cs) — capacity 100, pushed on successful submit, dedupes consecutive duplicates, ignores whitespace-only.
  - 8 tests in [SqlHistoryTests.cs](../src/ForEachDb.Tui.Tests/SqlHistoryTests.cs) cover push/older/newer, capacity eviction, draft preservation, index reset after push.

- [x] **8.2 History navigation**
  - `Ctrl+Up` / `Ctrl+Down` inside the SQL editor walks history. Current draft is preserved when navigating back to newest + 1.
  - Acceptance: unit tests above prove the logic; manual confirmation of key routing still needed.

- [x] **8.3 `SqlTokenizer`**
  - [Infrastructure/SqlTokenizer.cs](../src/ForEachDb.Tui/Infrastructure/SqlTokenizer.cs) — ~80 keywords, single-quoted strings with `''` escapes, dollar-quoted strings (including tagged `$body$…$body$`), `--` line comments, `/* */` block comments, decimal numbers, identifiers, punctuation.
  - 9 tests in [SqlTokenizerTests.cs](../src/ForEachDb.Tui.Tests/SqlTokenizerTests.cs) covering the happy paths plus unterminated strings and case-insensitive keyword matching.

- [ ] **8.4 Syntax-coloring editor** — **deferred**
  - Terminal.Gui v1's `TextView` applies a single `ColorScheme` to all text — per-token coloring would require a custom `View` that draws tokens and tracks its own cursor/selection. Substantial work for marginal benefit on a maintenance CLI.
  - The tokenizer is ready; plug it into a custom view when we upgrade to Terminal.Gui v2 (better rendering model) or when a user actually hits the need.

- [x] **8.5 Run key bindings**
  - `F5` is the hard binding (always works). `Ctrl+Enter` routes through `RootKeyEvent` as an alias where the terminal delivers it distinctly.
  - Acceptance: confirmed via code review; terminal-by-terminal manual check recommended.

### Phase 9 — Polish — ✅ complete

- [x] **9.1 Keybindings help overlay (F1)**
  - [HelpDialog.cs](../src/ForEachDb.Tui/Views/HelpDialog.cs) — modal listing every binding grouped by Run / Selection / SQL editor / Log + results / Recipes / App. Reachable via F1 and via Help → Key bindings menu.

- [x] **9.2 Empty-state handling**
  - "No databases found" — [ConnectionDialog.cs](../src/ForEachDb.Tui/Views/ConnectionDialog.cs) now surfaces "Connected — but no databases matched the current filters" and stays open.
  - "Nothing selected" / "Empty query" — logged inline with guidance when F5 is pressed.

- [x] **9.3 Deprecation note for `-i`**
  - [Options.cs](../src/ForEachDb/Options.cs) tags `-i` as `[Deprecated]` with the TUI as the replacement. README reflects this.

- [x] **9.4 README update**
  - TUI section near the top of [README.md](../README.md) with layout ASCII, keybindings table, results + CSV explainer, and recipes.

- [x] **9.5 CI sanity**
  - Added `coverlet.collector` to the new [ForEachDb.Tui.Tests.csproj](../src/ForEachDb.Tui.Tests/ForEachDb.Tui.Tests.csproj) so coverage reports include TUI tests. Release build succeeds; existing workflow picks up all four projects via the solution file without edits. The local [NuGet.config](../NuGet.config) pinning to `nuget.org` also removes the dependence on the private `ep` feed env vars in CI.

## Dependency graph (high level)

```
Phase 1 ──┬──> Phase 2 ──> Phase 3 ──> Phase 4 ──┬──> Phase 5 ──> Phase 6
          │                                       │
          └───────────────────────────────────────┴──> Phase 7 (recipes)
                                                        │
                                                        └──> Phase 8 (editor polish)
                                                              │
                                                              └──> Phase 9 (polish)
```

Phases 6 and 7 can run in parallel once Phase 5 lands. Phase 8 depends only on Phase 4.4 (the editor view) existing.

## Risks to watch

- **Terminal.Gui v1 vs v2**: we're on v1 (stable). v2 is in RC and brings a nicer view/layout model plus better TableView virtualization, but still churns. Revisit once v2 goes stable — the port should be mechanical.
- **Ctrl+Enter portability**: not all terminals deliver it. F5 is the hard binding.
- **Large result sets**: 100k+ rows in memory could pressure the process. Preview cap + streaming CSV mitigates but is not a real solution — consider a "streaming-only" mode in a later phase if people hit it.
- **Password handling in recipes**: zero storage is the safe default. If users push back, revisit with OS keychain integration rather than plain text.
- **SQL highlight scope creep**: stay at tokenizer level. A proper parser is a separate project.

## Out of scope (for now)

- Saved cross-session query history separate from recipes.
- Per-DB query templating (different code per DB).
- Result sort/filter inside the grid (export to CSV, do it there).
- Graphical charts / plots.
- Windows/Mac installers — `dotnet tool install` remains the distribution story.
