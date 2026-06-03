using EfGui.Engine;
using EfGui.Profiles;
using System.Threading.Tasks;

namespace EfGui.Actions;

public class ListMigrationsAction
{
    public async Task ExecuteAsync(EfCoreEngine engine, Profile profile)
    {
        await engine.RunAsync(profile, new[] { "migrations", "list" });
    }
}
