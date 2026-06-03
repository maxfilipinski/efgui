using EfGui.Services;
using System.Threading.Tasks;

namespace EfGui.Actions;

public class ListMigrationsAction
{
    public async Task ExecuteAsync(ProcessRunner runner, IConsole console)
    {
        await ActionsResolver.RunDotnetEfTool(runner, console, new ActionOptions
        {
            ActionName = "listing migrations",
            DotnetEfArgs = new[]
            {
                "migrations",
                "list"
            }
        });
    }
}
