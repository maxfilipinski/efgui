using System;

namespace EfGui.Profiles;

public enum DbConfigMode
{
    ConnectionString,
    CustomCode
}

public enum DbProvider
{
    SqlServer,
    PostgreSql,
    Sqlite,
    MySql
}

public class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public string CsprojPath { get; set; } = "";

    // Fully qualified, e.g. "MyApp.Data.AppDbContext".
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

    // C# statements with an "optionsBuilder" variable in scope; used in CustomCode mode.
    public string CustomCode { get; set; } = "";

    public Profile Clone() => (Profile)MemberwiseClone();
}
