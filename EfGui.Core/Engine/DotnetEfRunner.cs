using EfGui.Core.Profiles;
using EfGui.Core.Services;

namespace EfGui.Core.Engine;

public sealed class DotnetEfRunner : IDotnetEfRunner
{
    private readonly ProcessRunner _processRunner;
    private readonly IConsole _console;
    private readonly DotnetEfTool _tool;

    public DotnetEfRunner(ProcessRunner processRunner, IConsole console, DotnetEfTool tool)
    {
        _processRunner = processRunner;
        _console = console;
        _tool = tool;
    }

    public async Task<ProcessResult?> RunAsync(
        Profile profile,
        IReadOnlyList<string> efArgs,
        bool echoOutput = true,
        CancellationToken cancellationToken = default)
    {
        // Profiles are validated on save, but profiles.json can be edited by hand and the
        // DbContext name ends up in generated code.
        if (ProfileValidator.Validate(profile) is { } error)
        {
            _console.WriteLine(ConsoleMessageKind.Error, $"Profile '{profile.Name}' is invalid: {error}");
            return null;
        }

        var efExePath = await _tool.EnsureInstalledAsync(profile.DotnetEfVersion, cancellationToken);
        if (efExePath is null)
            return null;

        var helperCsproj = HelperProjectGenerator.Generate(profile);
        _console.WriteLine(ConsoleMessageKind.Info, $"Helper project: {helperCsproj}");

        // dotnet-ef does not restore the startup project, so restore + build it
        // (and the target project, transitively) ourselves.
        var build = await _processRunner.RunAsync(
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

        return await _processRunner.RunAsync(
            efExePath,
            args,
            workingDirectory: Path.GetDirectoryName(profile.CsprojPath),
            environment: new Dictionary<string, string?>
            {
                [HelperProjectGenerator.ConnectionStringVariable] = profile.ConnectionString
            },
            echoStdOut: echoOutput,
            cancellationToken: cancellationToken);
    }
}
