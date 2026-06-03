using EfGui.Engine;
using EfGui.Profiles;
using EfGui.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EfGui.Actions;

public class GenerateScriptAction
{
    public async Task ExecuteAsync(EfCoreEngine engine, IConsole console, Profile profile)
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"efgui-script-{DateTime.Now:yyyyMMdd'T'HHmmss}.sql");

        var result = await engine.RunAsync(profile, new[]
        {
            "migrations", "script",
            "--output", filePath
        });

        if (result?.Succeeded == true)
            console.WriteLine(ConsoleMessageKind.Info, $"Script written to: {filePath}");
    }
}
