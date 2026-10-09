namespace EfGui.Core;

public static class AppPaths
{
    private static readonly string LocalRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EfGui");

    private static readonly string RoamingRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EfGui");

    // Roaming: small user settings worth following the user across machines.
    public static string SettingsFile => Path.Combine(RoamingRoot, "profiles.json");

    // Local: machine-specific caches and generated artifacts.
    public static string ToolDir(string dotnetEfVersion) =>
        Path.Combine(LocalRoot, "tools", "dotnet-ef", dotnetEfVersion);

    public static string HelperDir(Guid profileId) =>
        Path.Combine(LocalRoot, "helpers", profileId.ToString("N"));

    public static string ScriptsDir => Path.Combine(LocalRoot, "scripts");

    public static string ErrorLogFile => Path.Combine(LocalRoot, "errors.log");
}
