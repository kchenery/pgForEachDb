# pgForEachDb
Allows a SQL statement to be run across multiple PostgreSQL databases on a server. Useful for commands such as ANALYZE.  Statements are run in parallel across multiple databases at a time.

# Usage
```
./ForEachDb --help
ForEachDb 1.0.0
Copyright (C) 2023 ForEachDb

  -q, --query              Required. Query to run against each database

  -h, --host               (Default: localhost) Hostname to connect to.

  -d, --database           (Default: postgres) Database to connect to.

  -u, --username           (Default: postgres) Username for the connection

  -p, --password           Password for the connection

  --port                   (Default: 5432) Password for the connection

  --ignore                 List of databases that should be ignored. E.g: --ignore foo bar baz

  --include-postgres-db    Flag to include the postgres database

  --include-template-db    Flag to include the postgres database

  --help                   Display this help screen.

  --version                Display version information.
```
