using EfGui.Services;
using System;
using System.Threading.Tasks;

namespace EfGui.Actions;

public class GenerateScriptAction
{
    public async Task ExecuteAsync(ProcessRunner runner, IConsole console)
    {
        var filePath = $"E:\\Projects\\EfGui\\Scripts\\efgui-script-{DateTime.Now:ddMMyyyy'T'HHmmss}.txt";

        await ActionsResolver.RunDotnetEfTool(runner, console, new ActionOptions
        {
            ActionName = "generating sql script",
            DotnetEfArgs = new[]
            {
                "migrations",
                "script",
                "--output",
                $"{filePath}"
            }
        });
    }
}
