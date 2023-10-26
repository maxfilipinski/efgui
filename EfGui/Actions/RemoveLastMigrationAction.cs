using System.Threading.Tasks;

namespace EfGui.Actions;

public class RemoveLastMigrationAction
{
    public async Task ExecuteAsync(Logger logger)
    {
        await ActionsResolver.RunDotnetEfTool(logger, new ActionOptions
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