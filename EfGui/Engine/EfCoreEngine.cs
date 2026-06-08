using EfGui.Profiles;
using EfGui.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EfGui.Engine;

public class EfCoreEngine
{
    private readonly ProcessRunner _runner;
    private readonly IConsole _console;
    private readonly DotnetEfTool _tool;

    public EfCoreEngine(ProcessRunner runner, IConsole console, DotnetEfTool tool)
    {
        _runner = runner;
        _console = console;
        _tool = tool;
    }

    public async Task<ProcessResult?> RunAsync(
        Profile profile,
        IReadOnlyList<string> efArgs,
        CancellationToken cancellationToken = default,
        bool echoOutput = true)
    {
        if (!File.Exists(profile.CsprojPath))
        {
            _console.WriteLine(ConsoleMessageKind.Error,
                $"Project file not found: {profile.CsprojPath}");
            return null;
        }

        var efExePath = await _tool.EnsureInstalledAsync(profile.DotnetEfVersion, cancellationToken);
        if (efExePath is null)
            return null;

        var helperCsproj = HelperProjectGenerator.Generate(profile);
        _console.WriteLine(ConsoleMessageKind.Info, $"Helper project: {helperCsproj}");

        // dotnet-ef does not restore the startup project, so restore + build it
        // (and the target project, transitively) ourselves.
        var build = await _runner.RunAsync(
            "dotnet",
            new[] { "build", helperCsproj, "-v", "minimal" },
            cancellationToken: cancellationToken);

        if (!build.Succeeded)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "Build failed; aborting.");
            return build;
        }

        var args = new List<string>(efArgs)
        {
            "--no-build",
            "--project", profile.CsprojPath,
            "--startup-project", helperCsproj,
            "--context", profile.DbContextName
        };

        return await _runner.RunAsync(
            efExePath,
            args,
            workingDirectory: Path.GetDirectoryName(profile.CsprojPath),
            echoStdOut: echoOutput,
            cancellationToken: cancellationToken);
    }
}
