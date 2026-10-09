using EfGui.Core.Processes;
using EfGui.Core.Profiles;

namespace EfGui.Core.DotnetEf;

public sealed class DotnetEfRunner : IDotnetEfRunner
{
    private readonly ProcessRunner _processRunner;
    private readonly IConsole _console;
    private readonly DotnetEfInstaller _installer;

    public DotnetEfRunner(ProcessRunner processRunner, IConsole console, DotnetEfInstaller installer)
    {
        _processRunner = processRunner;
        _console = console;
        _installer = installer;
    }

    public async Task<ProcessResult?> RunAsync(
        Profile profile,
        IReadOnlyList<string> efArgs,
        bool echoOutput = true,
        CancellationToken cancellationToken = default)
    {
        // Revalidate: profiles.json can be hand-edited, and the DbContext name ends up in generated code.
        if (ProfileValidator.Validate(profile) is { } error)
        {
            _console.WriteLine(ConsoleMessageKind.Error, $"Profile '{profile.Name}' is invalid: {error}");
            return null;
        }

        var efExePath = await _installer.EnsureInstalledAsync(profile.DotnetEfVersion, cancellationToken);
        if (efExePath is null)
        {
            return null;
        }

        var helperCsproj = HelperProjectGenerator.Generate(profile);
        _console.WriteLine(ConsoleMessageKind.Info, $"Helper project: {helperCsproj}");

        // dotnet-ef doesn't restore the startup project, so build it (and the target project) first.
        var build = await _processRunner.RunAsync(
            "dotnet",
            ["build", helperCsproj, "-v", "minimal"],
            cancellationToken: cancellationToken);

        if (!build.Succeeded)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "Build failed; aborting.");
            return build;
        }

        var csprojPath = profile.CsprojPath.Trim();
        List<string> args =
        [
            .. efArgs,
            "--no-build",
            "--project", csprojPath,
            "--startup-project", helperCsproj,
            "--context", profile.DbContextName
        ];

        return await _processRunner.RunAsync(
            efExePath,
            args,
            workingDirectory: Path.GetDirectoryName(csprojPath),
            environment: new Dictionary<string, string?>
            {
                [HelperProjectGenerator.ConnectionStringVariable] = profile.ConnectionString
            },
            echoStdOut: echoOutput,
            cancellationToken: cancellationToken);
    }
}
