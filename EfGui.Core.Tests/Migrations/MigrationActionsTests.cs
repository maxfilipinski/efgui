using EfGui.Core.Migrations;
using EfGui.Core.Profiles;
using EfGui.Core.Tests.Fakes;

namespace EfGui.Core.Tests.Migrations;

public sealed class MigrationActionsTests : IDisposable
{
    private readonly string _scriptsDir = Path.Combine(Path.GetTempPath(), "EfGuiTests", Guid.NewGuid().ToString("N"));
    private readonly FakeEfRunner _runner = new();
    private readonly RecordingConsole _console = new();
    private readonly Profile _profile = new() { Name = "P", MigrationsDir = "Data/Migrations" };
    private readonly MigrationActions _actions;

    public MigrationActionsTests() => _actions = new MigrationActions(_runner, _console, _scriptsDir);

    public void Dispose()
    {
        if (Directory.Exists(_scriptsDir))
        {
            Directory.Delete(_scriptsDir, recursive: true);
        }
    }

    // Ids follow EF's "<timestamp>_<Name>" shape; the name is the part after the underscore.
    private void GivenMigrations(params (string Id, bool Applied)[] items) =>
        _runner.Migrations.AddRange(items.Select(item => new MigrationInfo(item.Id, item.Id[(item.Id.IndexOf('_') + 1)..], item.Applied)));

    // The [from] [to] arguments between "migrations script" and "--output".
    private static string[] ScriptRange(IReadOnlyList<string> call)
    {
        Assert.Equal(new[] { "migrations", "script" }, call.Take(2));
        return call.Skip(2).TakeWhile(argument => argument != "--output").ToArray();
    }

    private static string OutputPath(IReadOnlyList<string> call) =>
        call[call.ToList().IndexOf("--output") + 1];

    [Fact]
    public void Old_scripts_are_deleted_and_recent_ones_kept()
    {
        Directory.CreateDirectory(_scriptsDir);
        var old = Path.Combine(_scriptsDir, "old.sql");
        var recent = Path.Combine(_scriptsDir, "recent.sql");
        var other = Path.Combine(_scriptsDir, "notes.txt");
        foreach (var file in new[] { old, recent, other })
        {
            File.WriteAllText(file, "");
        }

        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-31));
        File.SetLastWriteTimeUtc(other, DateTime.UtcNow.AddDays(-31));

        var deleted = _actions.DeleteScriptsOlderThan(TimeSpan.FromDays(30));

        Assert.Equal(1, deleted);
        Assert.False(File.Exists(old));
        Assert.True(File.Exists(recent));
        Assert.True(File.Exists(other));
    }

    [Fact]
    public void Script_cleanup_tolerates_missing_folder() =>
        Assert.Equal(0, _actions.DeleteScriptsOlderThan(TimeSpan.FromDays(30)));

    [Fact]
    public async Task Create_passes_trimmed_name_and_output_dir()
    {
        await _actions.CreateMigrationAsync(_profile, "  AddUsers ", TestContext.Current.CancellationToken);

        var call = Assert.Single(_runner.Calls);
        Assert.Equal(new[] { "migrations", "add", "AddUsers", "--output-dir", "Data/Migrations" }, call);
    }

    [Fact]
    public async Task Remove_never_forces()
    {
        await _actions.RemoveLastFromCodeAsync(_profile, TestContext.Current.CancellationToken);

        var call = Assert.Single(_runner.Calls);
        Assert.Equal(new[] { "migrations", "remove" }, call);
    }

    [Fact]
    public async Task Full_script_has_no_range_and_writes_into_scripts_dir()
    {
        var path = await _actions.GenerateFullScriptAsync(_profile, TestContext.Current.CancellationToken);

        var call = Assert.Single(_runner.Calls);
        Assert.Empty(ScriptRange(call));
        Assert.Equal(path, OutputPath(call));
        Assert.Equal(_scriptsDir, Path.GetDirectoryName(path));
        Assert.True(Directory.Exists(_scriptsDir));
    }

    [Fact]
    public async Task Apply_script_goes_from_previous_to_last_without_connecting()
    {
        GivenMigrations(("1_A", true), ("2_B", true), ("3_C", false));

        var path = await _actions.GenerateApplyScriptAsync(_profile, TestContext.Current.CancellationToken);

        Assert.NotNull(path);
        Assert.Contains("--no-connect", _runner.Calls[0]);
        Assert.Equal(new[] { "2_B", "3_C" }, ScriptRange(_runner.Calls[1]));
    }

    [Fact]
    public async Task Rollback_script_goes_from_last_to_previous()
    {
        GivenMigrations(("1_A", true), ("2_B", true));

        await _actions.GenerateRollbackScriptAsync(_profile, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "2_B", "1_A" }, ScriptRange(_runner.Calls[1]));
    }

    [Fact]
    public async Task Rollback_of_only_migration_targets_start()
    {
        GivenMigrations(("1_A", true));

        await _actions.GenerateRollbackScriptAsync(_profile, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "1_A", MigrationScriptRange.Start }, ScriptRange(_runner.Calls[1]));
    }

    [Fact]
    public async Task Unapplied_script_connects_and_goes_from_last_applied_to_last()
    {
        GivenMigrations(("1_A", true), ("2_B", false), ("3_C", false));

        await _actions.GenerateUnappliedScriptAsync(_profile, TestContext.Current.CancellationToken);

        Assert.DoesNotContain("--no-connect", _runner.Calls[0]);
        Assert.Equal(new[] { "1_A", "3_C" }, ScriptRange(_runner.Calls[1]));
    }

    [Fact]
    public async Task Unapplied_script_is_skipped_when_everything_is_applied()
    {
        GivenMigrations(("1_A", true), ("2_B", true));

        var path = await _actions.GenerateUnappliedScriptAsync(_profile, TestContext.Current.CancellationToken);

        Assert.Null(path);
        Assert.Single(_runner.Calls);
        Assert.True(_console.Contains(ConsoleMessageKind.Info, "No unapplied migrations"));
    }

    [Fact]
    public async Task Script_actions_stop_when_there_are_no_migrations()
    {
        Assert.Null(await _actions.GenerateApplyScriptAsync(_profile, TestContext.Current.CancellationToken));
        Assert.Null(await _actions.GenerateRollbackScriptAsync(_profile, TestContext.Current.CancellationToken));
        Assert.Null(await _actions.GenerateUnappliedScriptAsync(_profile, TestContext.Current.CancellationToken));

        Assert.All(_runner.Calls, call => Assert.Equal(new[] { "migrations", "list" }, call.Take(2)));
        Assert.True(_console.Contains(ConsoleMessageKind.Error, "No migrations found"));
    }

    [Fact]
    public async Task Failed_listing_is_not_reported_as_no_migrations()
    {
        _runner.Fail("migrations list");

        Assert.Null(await _actions.GenerateApplyScriptAsync(_profile, TestContext.Current.CancellationToken));
        Assert.Null(await _actions.RecreateAndGenerateScriptAsync(_profile, TestContext.Current.CancellationToken));

        Assert.False(_console.Contains(ConsoleMessageKind.Error, "No migrations found"));
    }

    [Fact]
    public async Task Failed_script_returns_null()
    {
        _runner.Fail("migrations script");

        Assert.Null(await _actions.GenerateFullScriptAsync(_profile, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Recreate_removes_readds_with_same_name_then_scripts_from_previous()
    {
        GivenMigrations(("1_A", true), ("2_AddUsers", false));

        var path = await _actions.RecreateAndGenerateScriptAsync(_profile, TestContext.Current.CancellationToken);

        Assert.NotNull(path);
        Assert.Equal(4, _runner.Calls.Count);
        Assert.Equal(new[] { "migrations", "remove" }, _runner.Calls[1]);
        Assert.Equal(new[] { "migrations", "add", "AddUsers", "--output-dir", "Data/Migrations" }, _runner.Calls[2]);
        Assert.Equal(new[] { "1_A" }, ScriptRange(_runner.Calls[3]));
    }

    [Fact]
    public async Task Recreate_stops_when_remove_fails()
    {
        GivenMigrations(("1_A", true));
        _runner.Fail("migrations remove");

        Assert.Null(await _actions.RecreateAndGenerateScriptAsync(_profile, TestContext.Current.CancellationToken));
        Assert.Equal(2, _runner.Calls.Count);
    }

    [Fact]
    public async Task Recreate_explains_recovery_when_readd_fails()
    {
        GivenMigrations(("1_AddUsers", false));
        _runner.Fail("migrations add");

        Assert.Null(await _actions.RecreateAndGenerateScriptAsync(_profile, TestContext.Current.CancellationToken));
        Assert.Equal(3, _runner.Calls.Count);
        Assert.True(_console.Contains(ConsoleMessageKind.Error, "was removed but could not be re-added"));
        Assert.True(_console.Contains(ConsoleMessageKind.Error, "'AddUsers'"));
    }
}
