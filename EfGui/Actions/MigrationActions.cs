using EfGui.Engine;
using EfGui.Profiles;
using EfGui.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EfGui.Actions;

public class MigrationActions
{
    private readonly EfCoreEngine _engine;
    private readonly IConsole _console;

    public MigrationActions(EfCoreEngine engine, IConsole console)
    {
        _engine = engine;
        _console = console;
    }

    public async Task CreateMigrationAsync(Profile profile, string name, CancellationToken cancellationToken = default)
    {
        await _engine.RunAsync(profile, new[]
        {
            "migrations", "add", name.Trim(),
            "--output-dir", profile.MigrationsDir
        }, cancellationToken);
    }

    public async Task ListMigrationsAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        await _engine.RunAsync(profile, new[] { "migrations", "list" }, cancellationToken);
    }

    public async Task VerifyAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        // dbcontext info loads the context through the design-time factory and prints
        // provider/connection details without touching migrations or the database schema.
        var result = await _engine.RunAsync(profile, new[] { "dbcontext", "info" }, cancellationToken);
        if (result?.Succeeded == true)
            _console.WriteLine(ConsoleMessageKind.Success, "Profile verified: project builds and the DbContext loads.");
    }

    public async Task GenerateFullScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        await GenerateScriptAsync(profile, from: null, to: null, "full", cancellationToken);
    }

    public async Task GenerateUnappliedScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var migrations = await GetMigrationsAsync(profile, connect: true, cancellationToken);
        if (migrations is null)
            return;

        if (migrations.Count == 0)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "No migrations found.");
            return;
        }

        if (!MigrationScriptRange.AnyUnapplied(migrations))
        {
            _console.WriteLine(ConsoleMessageKind.Info, "No unapplied migrations.");
            return;
        }

        await GenerateScriptAsync(
            profile, MigrationScriptRange.LastAppliedId(migrations), MigrationScriptRange.LastId(migrations),
            "unapplied", cancellationToken);
    }

    public async Task GenerateOptimizedModelAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        await _engine.RunAsync(profile, new[] { "dbcontext", "optimize" }, cancellationToken);
    }

    public async Task RemoveLastFromCodeAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        // --force: remove from code regardless of whether the migration was applied to the database.
        await _engine.RunAsync(profile, new[] { "migrations", "remove", "--force" }, cancellationToken);
    }

    public async Task RecreateAndGenerateScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var migrations = await GetMigrationsAsync(profile, connect: false, cancellationToken);
        if (migrations is null || migrations.Count == 0)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "No migrations found to recreate.");
            return;
        }

        var previous = MigrationScriptRange.PreviousId(migrations);
        var name = migrations[^1].Name;

        _console.WriteLine(ConsoleMessageKind.Info, $"Recreating migration '{name}'...");

        var removed = await _engine.RunAsync(profile, new[] { "migrations", "remove", "--force" }, cancellationToken);
        if (removed?.Succeeded != true)
            return;

        var added = await _engine.RunAsync(profile, new[]
        {
            "migrations", "add", name,
            "--output-dir", profile.MigrationsDir
        }, cancellationToken);
        if (added?.Succeeded != true)
            return;

        await GenerateScriptAsync(profile, previous, to: null, "recreated", cancellationToken);
    }

    public async Task GenerateApplyScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var migrations = await GetMigrationsAsync(profile, connect: false, cancellationToken);
        if (migrations is null || migrations.Count == 0)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "No migrations found.");
            return;
        }

        await GenerateScriptAsync(
            profile, MigrationScriptRange.PreviousId(migrations), MigrationScriptRange.LastId(migrations),
            "apply", cancellationToken);
    }

    public async Task GenerateRollbackScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var migrations = await GetMigrationsAsync(profile, connect: false, cancellationToken);
        if (migrations is null || migrations.Count == 0)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "No migrations found.");
            return;
        }

        await GenerateScriptAsync(
            profile, MigrationScriptRange.LastId(migrations), MigrationScriptRange.PreviousId(migrations),
            "rollback", cancellationToken);
    }

    private async Task<IReadOnlyList<MigrationInfo>?> GetMigrationsAsync(
        Profile profile, bool connect, CancellationToken cancellationToken)
    {
        var args = new List<string> { "migrations", "list", "--json", "--prefix-output" };
        if (!connect)
            args.Add("--no-connect");

        // Quiet: the JSON payload is for parsing, not for the user to read.
        var result = await _engine.RunAsync(profile, args, cancellationToken, echoOutput: false);
        if (result is not { Succeeded: true })
            return null;

        var parsed = MigrationListParser.Parse(result.StdOutLines);
        if (parsed is null)
            _console.WriteLine(ConsoleMessageKind.Error, "Could not parse the migration list.");

        return parsed;
    }

    private async Task GenerateScriptAsync(Profile profile, string? from, string? to, string label, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(AppPaths.ScriptsDir);
        var path = Path.Combine(
            AppPaths.ScriptsDir,
            $"{profile.Id:N}-{label}-{DateTime.Now:yyyyMMdd'T'HHmmss}.sql");

        var args = new List<string> { "migrations", "script" };
        if (from != null)
            args.Add(from);
        if (to != null)
            args.Add(to);
        args.Add("--output");
        args.Add(path);

        var result = await _engine.RunAsync(profile, args, cancellationToken);
        if (result?.Succeeded != true)
            return;

        _console.WriteLine(ConsoleMessageKind.Success, $"Script written to: {path}");
        TryOpenFile(path);
        _console.WriteLine(ConsoleMessageKind.Info, $"Folder: {AppPaths.ScriptsDir}");
    }

    private static void TryOpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // No associated application; the path and folder are printed to the console.
        }
    }
}
