using EfGui.Core.Engine;
using EfGui.Core.Profiles;
using EfGui.Core.Services;

namespace EfGui.Core.Tests.Fakes;

// Records every dotnet-ef invocation. Commands succeed unless configured otherwise;
// "migrations list" returns the configured migrations as --prefix-output JSON.
public sealed class FakeEfRunner : IDotnetEfRunner
{
    private readonly HashSet<string> _failing = new();

    public List<IReadOnlyList<string>> Calls { get; } = new();

    public List<MigrationInfo> Migrations { get; } = new();

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
            StdOutLines = command == "migrations list" ? ListOutput() : Array.Empty<string>()
        };
        return Task.FromResult<ProcessResult?>(result);
    }

    private IReadOnlyList<string> ListOutput()
    {
        var items = Migrations.Select(m =>
            $$"""{ "id": "{{m.Id}}", "name": "{{m.Name}}", "applied": {{(m.Applied ? "true" : "false")}} }""");
        return new[] { "data: [" + string.Join(", ", items) + "]" };
    }
}
