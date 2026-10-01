# EfGui

A desktop app for everyday Entity Framework Core migration work: create, list and remove
migrations, and generate SQL scripts, with one click instead of remembering `dotnet ef` flags.

Your project needs no changes. It doesn't need the EF Core Design package or a design-time
factory; EfGui provides both behind the scenes.

<!-- screenshot -->

## Getting started

1. Install the [.NET SDK](https://dotnet.microsoft.com/download) (10 or later).
2. Download EfGui from the [releases page](https://github.com/maxfilipinski/efgui/releases) (or, from
   a clone, run `dotnet run --project EfGui`), start it and click **+** next to the profile list.
3. Fill in the profile:
   - **Project**: the `.csproj` that contains your `DbContext` and migrations
   - **DbContext class**: its full name, e.g. `MyApp.Data.AppDbContext`
   - **Database configuration**: pick a provider (SQL Server, PostgreSQL, SQLite, MySQL) and enter
     a connection string, or choose **Custom code** and configure `optionsBuilder` yourself
   - The version fields should match the EF Core version your project uses
4. Click **Verify profile**. If the project builds and the context loads, you're set.

Create one profile per project and context; switch between them from the dropdown.

## Actions

| Action | What it does |
| --- | --- |
| Create migration | Adds a migration with the name you type |
| List migrations | Shows all migrations and whether they are applied |
| Generate full migration script | SQL for every migration, from an empty database |
| Generate unapplied migration script | SQL for the migrations the database doesn't have yet |
| Generate optimized model | Runs `dbcontext optimize` to create a compiled model |
| Remove from code | Deletes the last migration's files; refused if it's already applied |
| Recreate and generate script | Removes the last migration, re-adds it under the same name, and scripts it |
| Generate apply script | SQL that applies only the last migration |
| Generate rollback script | SQL that reverts only the last migration |

Generated scripts open automatically. Output from every command appears in the console on the
right; **Stop** cancels a running command, and Ctrl+scroll zooms the console.

## Where things are stored

| What | Where |
| --- | --- |
| Profiles and settings | `%APPDATA%\EfGui\profiles.json` |
| Generated SQL scripts (deleted after 30 days) | `%LOCALAPPDATA%\EfGui\scripts` |

Connection strings are encrypted for your Windows user. On other platforms they are stored as
plain text.

## Building the exe

```
dotnet publish EfGui -p:PublishProfile=win-x64
```

This produces a single self-contained `publish\win-x64\EfGui.exe` that runs without .NET
installed. Running migrations still needs the .NET SDK on the machine. The `linux-x64` and
`osx-arm64` profiles build the same for Linux and macOS; those builds are experimental and
untested.

Pushing a `v*` tag (for example `v1.0.0`) builds all three and attaches them to a GitHub release.
On macOS, the downloaded app isn't notarized, so run `xattr -d com.apple.quarantine EfGui` before
the first start.

## License

[MIT](LICENSE)
