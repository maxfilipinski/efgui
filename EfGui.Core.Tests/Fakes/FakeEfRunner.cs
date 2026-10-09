using EfGui.Core.DotnetEf;
using EfGui.Core.Migrations;
using EfGui.Core.Processes;
using EfGui.Core.Profiles;

namespace EfGui.Core.Tests.Fakes;

// Records calls; succeeds unless told to fail. "migrations list" returns Migrations as JSON.
public sealed class FakeEfRunner : IDotnetEfRunner
{
    private readonly HashSet<string> _failing = [];

    public List<IReadOnlyList<string>> Calls { get; } = [];

    public List<MigrationInfo> Migrations { get; } = [];

    public void Fail(string command) => _failing.Add(command);

    public Task<ProcessResult?> RunAsync(
        Profile profile,
        IReadOnlyList<string> efArgs,
        bool echoOutput = true,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(efArgs);
        var command = string.Join(' ', efArgs.Take(2));

        var result = new ProcessResult
        {
            ExitCode = _failing.Contains(command) ? 1 : 0,
            StdOutLines = command == "migrations list" ? ListOutput() : []
        };
        return Task.FromResult<ProcessResult?>(result);
    }

    private string[] ListOutput()
    {
        var items = Migrations.Select(migration =>
            $$"""{ "id": "{{migration.Id}}", "name": "{{migration.Name}}", "applied": {{(migration.Applied ? "true" : "false")}} }""");
        return ["data: [" + string.Join(", ", items) + "]"];
    }
}
