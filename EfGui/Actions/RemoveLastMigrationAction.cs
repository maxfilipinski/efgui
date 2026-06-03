using EfGui.Services;
using System.Threading.Tasks;

namespace EfGui.Actions;

public class RemoveLastMigrationAction
{
    public async Task ExecuteAsync(ProcessRunner runner, IConsole console)
    {
        await ActionsResolver.RunDotnetEfTool(runner, console, new ActionOptions
        {
            ActionName = "removing last migration",
            DotnetEfArgs = new[]
            {
                "migrations",
                "remove",
                "--json"
            }
        });
    }
}
