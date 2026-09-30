# EfGui

A desktop GUI for common `dotnet ef` migration tasks, built with Avalonia.

The target project needs no changes. It doesn't need `Microsoft.EntityFrameworkCore.Design`
or an `IDesignTimeDbContextFactory`: EfGui generates a small helper startup project per profile
that adds both, then runs a pinned `dotnet-ef` against your project through it.

## Features

- Profiles per project/DbContext, with either a connection string (SQL Server, PostgreSQL,
  SQLite, MySQL) or custom C# code to configure the context
- Create, list, and remove migrations; recreate the last migration
- Generate SQL scripts: full, unapplied (checked against the database), apply and rollback
  for the last migration
- Verify a profile (`dbcontext info`) and generate a compiled model (`dbcontext optimize`)

## Requirements

- .NET 10 SDK
- Windows is the primary target. Connection strings are encrypted at rest with DPAPI there;
  on other platforms they are stored as plain text.

## Build and run

```
dotnet run --project EfGui
dotnet test
```

## Data locations

| What | Where |
| --- | --- |
| Profiles and UI settings | `%APPDATA%\EfGui\profiles.json` |
| Pinned `dotnet-ef` installs | `%LOCALAPPDATA%\EfGui\tools\dotnet-ef\<version>` |
| Generated helper projects | `%LOCALAPPDATA%\EfGui\helpers\<profile-id>` |
| Generated SQL scripts | `%LOCALAPPDATA%\EfGui\scripts` |

## Project layout

| Project | Contents |
| --- | --- |
| `EfGui` | Avalonia app: views, view models, console rendering |
| `EfGui.Core` | UI-independent logic: profiles, helper project generation, `dotnet-ef` invocation, output parsing |
| `EfGui.Tests` | xUnit tests for `EfGui.Core` |
