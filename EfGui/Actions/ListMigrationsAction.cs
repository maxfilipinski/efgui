using System.Threading.Tasks;

namespace EfGui.Actions;

public class ListMigrationsAction
{
    public async Task ExecuteAsync(Logger logger)
    {
        await ActionsResolver.RunDotnetEfTool(logger, new ActionOptions
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