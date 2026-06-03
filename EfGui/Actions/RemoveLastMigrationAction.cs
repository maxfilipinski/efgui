using EfGui.Engine;
using EfGui.Profiles;
using System.Threading.Tasks;

namespace EfGui.Actions;

public class RemoveLastMigrationAction
{
    public async Task ExecuteAsync(EfCoreEngine engine, Profile profile)
    {
        // --force: remove from code regardless of whether the migration was applied to the database.
        await engine.RunAsync(profile, new[] { "migrations", "remove", "--force" });
    }
}
