using EfGui.Core.Processes;
using EfGui.Core.Profiles;

namespace EfGui.Core.DotnetEf;

public interface IDotnetEfRunner
{
    // Null when the run could not start (invalid profile, dotnet-ef install failure).
    Task<ProcessResult?> RunAsync(
        Profile profile,
        IReadOnlyList<string> efArgs,
        bool echoOutput = true,
        CancellationToken cancellationToken = default);
}
