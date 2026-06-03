using EfGui.Services;
using System.Threading.Tasks;

namespace EfGui.Actions;

public class AddMigrationAction
{
    public async Task ExecuteAsync(ProcessRunner runner, IConsole console)
    {
        var migrationName = "example";

        await ActionsResolver.RunDotnetEfTool(runner, console, new ActionOptions
        {
            ActionName = $"adding migration '{migrationName}'",
            DotnetEfArgs = new[]
            {
                "migrations",
                "add",
                migrationName,
                "--json"
            }
        });
    }
}
