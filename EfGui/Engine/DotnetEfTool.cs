using EfGui.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EfGui.Engine;

public class DotnetEfTool
{
    private readonly ProcessRunner _runner;
    private readonly IConsole _console;

    public DotnetEfTool(ProcessRunner runner, IConsole console)
    {
        _runner = runner;
        _console = console;
    }

    // Returns the path to the pinned dotnet-ef executable, installing it on first use.
    public async Task<string?> EnsureInstalledAsync(string version, CancellationToken cancellationToken = default)
    {
        var toolDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EfGui", "tools", "dotnet-ef", version);
        var exePath = Path.Combine(toolDir, OperatingSystem.IsWindows() ? "dotnet-ef.exe" : "dotnet-ef");

        if (File.Exists(exePath))
            return exePath;

        _console.WriteLine(ConsoleMessageKind.Info, $"Installing dotnet-ef {version}...");

        var result = await _runner.RunAsync(
            "dotnet",
            new[] { "tool", "install", "dotnet-ef", "--version", version, "--tool-path", toolDir },
            cancellationToken: cancellationToken);

        if (!result.Succeeded || !File.Exists(exePath))
        {
            _console.WriteLine(ConsoleMessageKind.Error, $"Failed to install dotnet-ef {version}.");
            return null;
        }

        return exePath;
    }
}
