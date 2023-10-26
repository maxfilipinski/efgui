using System.Threading.Tasks;

namespace EfGui.Actions;

public class AddMigrationAction
{
    public async Task ExecuteAsync(Logger logger)
    {
        var migrationName = "example";

        await ActionsResolver.RunDotnetEfTool(logger, new ActionOptions
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