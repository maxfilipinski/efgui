using CliWrap;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EfGui.Services;

public class ProcessResult
{
    public required int ExitCode { get; init; }
    public required IReadOnlyList<string> StdOutLines { get; init; }

    public bool Succeeded => ExitCode == 0;
}

public class ProcessRunner
{
    private readonly IConsole _console;

    public ProcessRunner(IConsole console)
    {
        _console = console;
    }

    public async Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string?>? environment = null,
        bool echoStdOut = true,
        CancellationToken cancellationToken = default)
    {
        _console.WriteLine(ConsoleMessageKind.Command, $"> {executable} {string.Join(' ', arguments)}");

        var stdOutLines = new List<string>();

        var command = Cli.Wrap(executable)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                stdOutLines.Add(line);
                if (echoStdOut)
                    _console.WriteLine(ConsoleMessageKind.StdOut, line);
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
                _console.WriteLine(ConsoleMessageKind.StdErr, line)));

        if (workingDirectory != null)
            command = command.WithWorkingDirectory(workingDirectory);

        if (environment != null)
            command = command.WithEnvironmentVariables(env =>
            {
                foreach (var (key, value) in environment)
                    env.Set(key, value);
            });

        try
        {
            var result = await command.ExecuteAsync(cancellationToken);

            _console.WriteLine(
                result.ExitCode == 0 ? ConsoleMessageKind.Success : ConsoleMessageKind.Error,
                $"Process exited with code {result.ExitCode}.");

            return new ProcessResult { ExitCode = result.ExitCode, StdOutLines = stdOutLines };
        }
        catch (OperationCanceledException)
        {
            _console.WriteLine(ConsoleMessageKind.Error, "Operation cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            // E.g. executable not found.
            _console.WriteLine(ConsoleMessageKind.Error, ex.Message);
            return new ProcessResult { ExitCode = -1, StdOutLines = stdOutLines };
        }
    }
}
