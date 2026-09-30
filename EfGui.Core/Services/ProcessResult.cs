namespace EfGui.Core.Services;

public sealed class ProcessResult
{
    public required int ExitCode { get; init; }
    public required IReadOnlyList<string> StdOutLines { get; init; }

    public bool Succeeded => ExitCode == 0;
}
