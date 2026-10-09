using EfGui.Core.DotnetEf;
using EfGui.Core.Processes;
using EfGui.Core.Profiles;

namespace EfGui.Core.Migrations;

public sealed class MigrationActions
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

    public int DeleteScriptsOlderThan(TimeSpan age)
    {
        if (!Directory.Exists(_scriptsDir))
        {
            return 0;
        }

        var cutoff = DateTime.UtcNow - age;
        var deleted = 0;
        foreach (var file in Directory.EnumerateFiles(_scriptsDir, "*.sql"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) >= cutoff)
                {
                    continue;
                }

                File.Delete(file);
                deleted++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Locked, e.g. open in an editor; retried on the next start.
            }
        }

        return deleted;
    }

    public async Task CreateMigrationAsync(Profile profile, string name, CancellationToken cancellationToken = default)
    {
        await AddMigrationAsync(profile, name.Trim(), cancellationToken);
    }

    public async Task ListMigrationsAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        await _efRunner.RunAsync(profile, ["migrations", "list"], cancellationToken: cancellationToken);
    }

    public async Task VerifyAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        // dbcontext info loads the context through the factory without touching migrations or the schema.
        var result = await _efRunner.RunAsync(profile, ["dbcontext", "info"], cancellationToken: cancellationToken);
        if (result is { Succeeded: true })
        {
            _console.WriteLine(ConsoleMessageKind.Success, "Profile verified: project builds and the DbContext loads.");
        }
    }

    public Task<string?> GenerateFullScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        return GenerateScriptAsync(profile, from: null, to: null, "full", cancellationToken);
    }

    public async Task<string?> GenerateUnappliedScriptAsync(Profile profile,
        CancellationToken cancellationToken = default)
    {
        if (await GetNonEmptyMigrationsAsync(profile, connect: true, "No migrations found.", cancellationToken)
            is not { } migrations)
        {
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
        await _efRunner.RunAsync(profile, ["dbcontext", "optimize"], cancellationToken: cancellationToken);
    }

    public async Task RemoveLastFromCodeAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        await RemoveLastMigrationAsync(profile, cancellationToken);
    }

    public async Task<string?> RecreateAndGenerateScriptAsync(Profile profile,
        CancellationToken cancellationToken = default)
    {
        if (await GetNonEmptyMigrationsAsync(profile, connect: false, "No migrations found to recreate.",
                cancellationToken) is not { } migrations)
        {
            return null;
        }

        var previous = MigrationScriptRange.PreviousId(migrations);
        var name = migrations[^1].Name;

        _console.WriteLine(ConsoleMessageKind.Info, $"Recreating migration '{name}'...");

        if (await RemoveLastMigrationAsync(profile, cancellationToken) is not { Succeeded: true })
        {
            return null;
        }

        if (await AddMigrationAsync(profile, name, cancellationToken) is not { Succeeded: true })
        {
            _console.WriteLine(ConsoleMessageKind.Error,
                $"Migration '{name}' was removed but could not be re-added. "
                + $"Fix the error above, then create it again with the name '{name}'.");
            return null;
        }

        return await GenerateScriptAsync(profile, previous, to: null, "recreated", cancellationToken);
    }

    public async Task<string?> GenerateApplyScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        if (await GetNonEmptyMigrationsAsync(profile, connect: false, "No migrations found.", cancellationToken)
            is not { } migrations)
        {
            return null;
        }

        return await GenerateScriptAsync(
            profile, MigrationScriptRange.PreviousId(migrations), MigrationScriptRange.LastId(migrations),
            "apply", cancellationToken);
    }

    public async Task<string?> GenerateRollbackScriptAsync(Profile profile,
        CancellationToken cancellationToken = default)
    {
        if (await GetNonEmptyMigrationsAsync(profile, connect: false, "No migrations found.", cancellationToken)
            is not { } migrations)
        {
            return null;
        }

        return await GenerateScriptAsync(
            profile, MigrationScriptRange.LastId(migrations), MigrationScriptRange.PreviousId(migrations),
            "rollback", cancellationToken);
    }

    private Task<ProcessResult?> AddMigrationAsync(Profile profile, string name, CancellationToken cancellationToken)
    {
        return _efRunner.RunAsync(profile, ["migrations", "add", name, "--output-dir", profile.MigrationsDir],
            cancellationToken: cancellationToken);
    }

    // No --force: it would also revert an applied migration in the database.
    private Task<ProcessResult?> RemoveLastMigrationAsync(Profile profile, CancellationToken cancellationToken)
    {
        return _efRunner.RunAsync(profile, ["migrations", "remove"], cancellationToken: cancellationToken);
    }

    // Null when listing failed (already reported) or found nothing.
    private async Task<IReadOnlyList<MigrationInfo>?> GetNonEmptyMigrationsAsync(
        Profile profile, bool connect, string emptyMessage, CancellationToken cancellationToken)
    {
        var migrations = await GetMigrationsAsync(profile, connect, cancellationToken);
        if (migrations is { Count: 0 })
        {
            _console.WriteLine(ConsoleMessageKind.Error, emptyMessage);
            return null;
        }

        return migrations;
    }

    private async Task<IReadOnlyList<MigrationInfo>?> GetMigrationsAsync(
        Profile profile, bool connect, CancellationToken cancellationToken)
    {
        List<string> args = ["migrations", "list", "--json", "--prefix-output"];
        if (!connect)
        {
            args.Add("--no-connect");
        }

        var result = await _efRunner.RunAsync(profile, args, echoOutput: false, cancellationToken);
        if (result is not { Succeeded: true })
        {
            return null;
        }

        var parsed = MigrationListParser.Parse(result.StdOutLines);
        if (parsed is null)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "Could not parse the migration list.");
        }

        return parsed;
    }

    private async Task<string?> GenerateScriptAsync(
        Profile profile,
        string? from,
        string? to,
        string label,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_scriptsDir);
        var path = Path.Combine(
            _scriptsDir,
            $"{profile.Id:N}-{label}-{DateTime.Now:yyyyMMdd'T'HHmmss}.sql");

        List<string> args = ["migrations", "script"];
        if (from != null)
        {
            args.Add(from);
        }

        if (to != null)
        {
            args.Add(to);
        }

        args.Add("--output");
        args.Add(path);

        var result = await _efRunner.RunAsync(profile, args, cancellationToken: cancellationToken);
        if (result is not { Succeeded: true })
        {
            return null;
        }

        _console.WriteLine(ConsoleMessageKind.Success, $"Script written to: {path}");
        _console.WriteLine(ConsoleMessageKind.Info, $"Folder: {_scriptsDir}");
        return path;
    }
}
