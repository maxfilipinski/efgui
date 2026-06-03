using EfGui.Engine;
using EfGui.Profiles;
using System.Threading.Tasks;

namespace EfGui.Actions;

public class AddMigrationAction
{
    public async Task ExecuteAsync(EfCoreEngine engine, Profile profile, string migrationName)
    {
        await engine.RunAsync(profile, new[]
        {
            "migrations", "add", migrationName,
            "--output-dir", profile.MigrationsDir
        });
    }
}
