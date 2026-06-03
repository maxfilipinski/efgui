using EfGui.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EfGui.Actions;

// Interim plumbing: hardcoded target until the helper-project engine replaces it.
public static class ActionsResolver
{
    public static string exampleProjectPath = "E:\\Projects\\ExampleProject\\ExampleProject";
    public static string appContextName = "AppContext";

    public static async Task RunDotnetEfTool(ProcessRunner runner, IConsole console, ActionOptions options)
    {
        console.WriteLine(ConsoleMessageKind.Info, $"Started operation: {options.ActionName}");

        var args = new List<string>(options.DotnetEfArgs)
        {
            "--context", appContextName,
            "--project", exampleProjectPath
        };

        await runner.RunAsync("dotnet-ef", args);
    }
}

public class ActionOptions
{
    public required string ActionName { get; init; }
    public required string[] DotnetEfArgs { get; init; }
}
