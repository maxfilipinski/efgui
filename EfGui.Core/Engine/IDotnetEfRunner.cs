using EfGui.Core.Profiles;
using EfGui.Core.Services;

namespace EfGui.Core.Engine;

public interface IDotnetEfRunner
{
    // Null when the run could not start (invalid profile, dotnet-ef install failure).
    Task<ProcessResult?> RunAsync(
        Profile profile,
        IReadOnlyList<string> efArgs,
        bool echoOutput = true,
        CancellationToken cancellationToken = default);
}
