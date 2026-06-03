using EfGui.Engine;
using EfGui.Profiles;
using EfGui.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EfGui.Actions;

public partial class MigrationActions
{
    // Matches "20260603123015_AddColumns" optionally suffixed with " (Pending)".
    [GeneratedRegex(@"^(?<id>\d{14}_\S+?)(?<pending> \(Pending\))?$")]
    private static partial Regex MigrationLineRegex();

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

        if (migrations.All(m => !m.Pending))
        {
            _console.WriteLine(ConsoleMessageKind.Info, "No unapplied migrations.");
            return;
        }

        var lastApplied = migrations.LastOrDefault(m => !m.Pending)?.Id ?? "0";
        await GenerateScriptAsync(profile, lastApplied, migrations[^1].Id, "unapplied", cancellationToken);
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

        var last = migrations[^1].Id;
        var previous = migrations.Count >= 2 ? migrations[^2].Id : "0";
        var name = last.Split('_', 2)[1];

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

        var previous = migrations.Count >= 2 ? migrations[^2].Id : "0";
        await GenerateScriptAsync(profile, previous, migrations[^1].Id, "apply", cancellationToken);
    }

    public async Task GenerateRollbackScriptAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var migrations = await GetMigrationsAsync(profile, connect: false, cancellationToken);
        if (migrations is null || migrations.Count == 0)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "No migrations found.");
            return;
        }

        var previous = migrations.Count >= 2 ? migrations[^2].Id : "0";
        await GenerateScriptAsync(profile, migrations[^1].Id, previous, "rollback", cancellationToken);
    }

    private record MigrationEntry(string Id, bool Pending);

    private async Task<List<MigrationEntry>?> GetMigrationsAsync(Profile profile, bool connect, CancellationToken cancellationToken)
    {
        var args = new List<string> { "migrations", "list" };
        if (!connect)
            args.Add("--no-connect");

        var result = await _engine.RunAsync(profile, args, cancellationToken);
        if (result is not { Succeeded: true })
            return null;

        if (connect && result.StdOutLines.Any(l => l.Contains("Pending status not shown")))
        {
            _console.WriteLine(ConsoleMessageKind.Error,
                "Could not reach the database to determine applied migrations.");
            return null;
        }

        return result.StdOutLines
            .Select(l => MigrationLineRegex().Match(l.Trim()))
            .Where(m => m.Success)
            .Select(m => new MigrationEntry(m.Groups["id"].Value, m.Groups["pending"].Success))
            .ToList();
    }

    private async Task GenerateScriptAsync(Profile profile, string? from, string? to, string label, CancellationToken cancellationToken)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"efgui-{label}-script-{DateTime.Now:yyyyMMdd'T'HHmmss}.sql");

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

        _console.WriteLine(ConsoleMessageKind.Info, $"Script written to: {path}");
        TryOpenFile(path);
    }

    private static void TryOpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // No associated application; the path was already printed to the console.
        }
    }
}
