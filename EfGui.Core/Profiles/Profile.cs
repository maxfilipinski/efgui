namespace EfGui.Core.Profiles;

public sealed class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public string CsprojPath { get; set; } = "";

    public string DbContextName { get; set; } = "";

    // Relative to the project directory.
    public string MigrationsDir { get; set; } = "Migrations";

    // Used for the generated helper project.
    public string TargetFramework { get; set; } = "net10.0";

    public string DotnetEfVersion { get; set; } = "10.0.8";

    public string EfCoreDesignVersion { get; set; } = "10.0.8";

    public DbConfigMode DbConfigMode { get; set; } = DbConfigMode.ConnectionString;

    public DbProvider DbProvider { get; set; } = DbProvider.SqlServer;

    // Empty means "same as EfCoreDesignVersion".
    public string ProviderPackageVersion { get; set; } = "";

    public string ConnectionString { get; set; } = "";

    public string CustomCode { get; set; } = "";

    public Profile Clone() => (Profile)MemberwiseClone();
}
