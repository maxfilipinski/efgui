using EfGui.Core.Processes;

namespace EfGui.Core.DotnetEf;

public sealed class DotnetEfInstaller
{
    private readonly ProcessRunner _processRunner;
    private readonly IConsole _console;

    public DotnetEfInstaller(ProcessRunner processRunner, IConsole console)
    {
        _processRunner = processRunner;
        _console = console;
    }

    public async Task<string?> EnsureInstalledAsync(string version, CancellationToken cancellationToken = default)
    {
        var toolDir = AppPaths.ToolDir(version);
        var exePath = Path.Combine(toolDir, OperatingSystem.IsWindows() ? "dotnet-ef.exe" : "dotnet-ef");

        if (File.Exists(exePath))
        {
            return exePath;
        }

        _console.WriteLine(ConsoleMessageKind.Info, $"Installing dotnet-ef {version}...");

        var result = await _processRunner.RunAsync(
            "dotnet",
            ["tool", "install", "dotnet-ef", "--version", version, "--tool-path", toolDir],
            cancellationToken: cancellationToken);

        if (!result.Succeeded || !File.Exists(exePath))
        {
            _console.WriteLine(ConsoleMessageKind.Error, $"Failed to install dotnet-ef {version}.");
            return null;
        }

        return exePath;
    }
}
