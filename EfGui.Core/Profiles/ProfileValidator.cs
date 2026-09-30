using EfGui.Core.Engine;

namespace EfGui.Core.Profiles;

public static class ProfileValidator
{
    // Returns the first problem found, or null when the profile is usable.
    public static string? Validate(Profile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
            return "Profile name is required.";

        if (string.IsNullOrWhiteSpace(profile.CsprojPath))
            return "Project path is required.";

        if (!profile.CsprojPath.Trim().EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return "Project path must point to a .csproj file.";

        if (!File.Exists(profile.CsprojPath.Trim()))
            return "Project file does not exist.";

        if (string.IsNullOrWhiteSpace(profile.DbContextName))
            return "DbContext class name is required.";

        if (!CSharpIdentifier.IsValidQualified(profile.DbContextName))
            return "DbContext class name must be a valid C# type name, e.g. MyApp.Data.AppDbContext.";

        if (string.IsNullOrWhiteSpace(profile.MigrationsDir))
            return "Migrations directory is required.";

        if (string.IsNullOrWhiteSpace(profile.TargetFramework))
            return "Target framework is required.";

        if (string.IsNullOrWhiteSpace(profile.DotnetEfVersion))
            return "dotnet-ef version is required.";

        if (string.IsNullOrWhiteSpace(profile.EfCoreDesignVersion))
            return "EF Core Design version is required.";

        if (profile.DbConfigMode == DbConfigMode.ConnectionString && string.IsNullOrWhiteSpace(profile.ConnectionString))
            return "Connection string is required.";

        if (profile.DbConfigMode == DbConfigMode.CustomCode && string.IsNullOrWhiteSpace(profile.CustomCode))
            return "Configuration code is required.";

        return null;
    }
}
