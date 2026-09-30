using EfGui.Core.Engine;
using EfGui.Core.Profiles;
using EfGui.Core.Services;

namespace EfGui.Core.Actions;

public class MigrationActions
{
    private readonly IDotnetEfRunner _efRunner;
    private readonly IConsole _console;
    private readonly string _scriptsDir;

    public MigrationActions(IDotnetEfRunner efRunner, IConsole console, string scriptsDir)
    {
        _efRunner = efRunner;
        _console = console;
        _scriptsDir = scriptsDir;
    }

    public async Task CreateMigrationAsync(Profile profile, string name, CancellationToken cancellationToken = default)
    {
        await _efRunner.RunAsync(profile, new[]
        {
            "migrations", "add", name.Trim(),
            "--output-dir", profile.MigrationsDir
        }, cancellationToken: cancellationToken);
    }

    public async Task ListMigrationsAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        await _efRunner.RunAsync(profile, new[] { "migrations", "list" }, cancellationToken: cancellationToken);
    }

    public async Task VerifyAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        // dbcontext info loads the context through the design-time factory and prints
        // provider/connection details without touching migrations or the database schema.
        var result = await _efRunner.RunAsync(profile, new[] { "dbcontext", "info" }, cancellationToken: cancellationToken);
        if (result?.Succeeded == true)
            _console.WriteLine(ConsoleMessageKind.Success, "Profile verified: project builds and the DbContext loads.");
    }

    public Task<string?> GenerateFullScriptAsync(Profile profile, CancellationToken cancellationToken = default) =>
        GenerateScriptAsync(profile, from: null, to: null, "full", cancellationToken);

    public async Task<string?> GenerateUnappliedScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var migrations = await GetMigrationsAsync(profile, connect: true, cancellationToken);
        if (migrations is null)
            return null;

        if (migrations.Count == 0)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "No migrations found.");
            return null;
        }

        if (!MigrationScriptRange.AnyUnapplied(migrations))
        {
            _console.WriteLine(ConsoleMessageKind.Info, "No unapplied migrations.");
            return null;
        }

        return await GenerateScriptAsync(
            profile, MigrationScriptRange.LastAppliedId(migrations), MigrationScriptRange.LastId(migrations),
            "unapplied", cancellationToken);
    }

    public async Task GenerateOptimizedModelAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        await _efRunner.RunAsync(profile, new[] { "dbcontext", "optimize" }, cancellationToken: cancellationToken);
    }

    public async Task RemoveLastFromCodeAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        // No --force: that would also revert the migration in the database. Without it,
        // EF refuses to remove a migration that has already been applied.
        await _efRunner.RunAsync(profile, new[] { "migrations", "remove" }, cancellationToken: cancellationToken);
    }

    public async Task<string?> RecreateAndGenerateScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var migrations = await GetMigrationsAsync(profile, connect: false, cancellationToken);
        if (migrations is null || migrations.Count == 0)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "No migrations found to recreate.");
            return null;
        }

        var previous = MigrationScriptRange.PreviousId(migrations);
        var name = migrations[^1].Name;

        _console.WriteLine(ConsoleMessageKind.Info, $"Recreating migration '{name}'...");

        var removed = await _efRunner.RunAsync(profile, new[] { "migrations", "remove" }, cancellationToken: cancellationToken);
        if (removed?.Succeeded != true)
            return null;

        var added = await _efRunner.RunAsync(profile, new[]
        {
            "migrations", "add", name,
            "--output-dir", profile.MigrationsDir
        }, cancellationToken: cancellationToken);
        if (added?.Succeeded != true)
        {
            _console.WriteLine(ConsoleMessageKind.Error,
                $"Migration '{name}' was removed but could not be re-added. Fix the error above, then create it again with the name '{name}'.");
            return null;
        }

        return await GenerateScriptAsync(profile, previous, to: null, "recreated", cancellationToken);
    }

    public async Task<string?> GenerateApplyScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var migrations = await GetMigrationsAsync(profile, connect: false, cancellationToken);
        if (migrations is null || migrations.Count == 0)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "No migrations found.");
            return null;
        }

        return await GenerateScriptAsync(
            profile, MigrationScriptRange.PreviousId(migrations), MigrationScriptRange.LastId(migrations),
            "apply", cancellationToken);
    }

    public async Task<string?> GenerateRollbackScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var migrations = await GetMigrationsAsync(profile, connect: false, cancellationToken);
        if (migrations is null || migrations.Count == 0)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "No migrations found.");
            return null;
        }

        return await GenerateScriptAsync(
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
        var result = await _efRunner.RunAsync(profile, args, echoOutput: false, cancellationToken);
        if (result is not { Succeeded: true })
            return null;

        var parsed = MigrationListParser.Parse(result.StdOutLines);
        if (parsed is null)
            _console.WriteLine(ConsoleMessageKind.Error, "Could not parse the migration list.");

        return parsed;
    }

    // Returns the script path, or null when generation failed.
    private async Task<string?> GenerateScriptAsync(Profile profile, string? from, string? to, string label, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_scriptsDir);
        var path = Path.Combine(
            _scriptsDir,
            $"{profile.Id:N}-{label}-{DateTime.Now:yyyyMMdd'T'HHmmss}.sql");

        var args = new List<string> { "migrations", "script" };
        if (from != null)
            args.Add(from);
        if (to != null)
            args.Add(to);
        args.Add("--output");
        args.Add(path);

        var result = await _efRunner.RunAsync(profile, args, cancellationToken: cancellationToken);
        if (result?.Succeeded != true)
            return null;

        _console.WriteLine(ConsoleMessageKind.Success, $"Script written to: {path}");
        _console.WriteLine(ConsoleMessageKind.Info, $"Folder: {_scriptsDir}");
        return path;
    }
}
