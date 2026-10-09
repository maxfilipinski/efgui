using System.Text.RegularExpressions;

namespace EfGui.Core.Profiles;

public static partial class ProfileValidator
{
    public static string? Validate(Profile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            return "Profile name is required.";
        }

        if (string.IsNullOrWhiteSpace(profile.CsprojPath))
        {
            return "Project path is required.";
        }

        if (!profile.CsprojPath.Trim().EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return "Project path must point to a .csproj file.";
        }

        if (!File.Exists(profile.CsprojPath.Trim()))
        {
            return "Project file does not exist.";
        }

        if (string.IsNullOrWhiteSpace(profile.DbContextName))
        {
            return "DbContext class name is required.";
        }

        if (!CSharpIdentifier.IsValidQualified(profile.DbContextName))
        {
            return "DbContext class name must be a valid C# type name, e.g. MyApp.Data.AppDbContext.";
        }

        if (string.IsNullOrWhiteSpace(profile.MigrationsDir))
        {
            return "Migrations directory is required.";
        }

        if (string.IsNullOrWhiteSpace(profile.TargetFramework))
        {
            return "Target framework is required.";
        }

        if (string.IsNullOrWhiteSpace(profile.DotnetEfVersion))
        {
            return "dotnet-ef version is required.";
        }

        if (!IsExactVersion(profile.DotnetEfVersion))
        {
            return "dotnet-ef version must be an exact version, e.g. 10.0.8.";
        }

        if (string.IsNullOrWhiteSpace(profile.EfCoreDesignVersion))
        {
            return "EF Core Design version is required.";
        }

        if (!IsExactVersion(profile.EfCoreDesignVersion))
        {
            return "EF Core Design version must be an exact version, e.g. 10.0.8.";
        }

        if (profile.ProviderPackageVersion.Length > 0 && !IsExactVersion(profile.ProviderPackageVersion))
        {
            return "Provider package version must be an exact version, e.g. 10.0.8, or empty.";
        }

        // Enums from a hand-edited profiles.json can hold any number.
        if (!Enum.IsDefined(profile.DbConfigMode))
        {
            return "Unknown database configuration mode.";
        }

        if (profile.DbConfigMode == DbConfigMode.ConnectionString && !Enum.IsDefined(profile.DbProvider))
        {
            return "Unknown database provider.";
        }

        if (profile.DbConfigMode == DbConfigMode.ConnectionString && string.IsNullOrWhiteSpace(profile.ConnectionString))
        {
            return "Connection string is required.";
        }

        if (profile.DbConfigMode == DbConfigMode.CustomCode && string.IsNullOrWhiteSpace(profile.CustomCode))
        {
            return "Configuration code is required.";
        }

        return null;
    }

    private static bool IsExactVersion(string version)
    {
        return ExactVersionPattern().IsMatch(version);
    }

    [GeneratedRegex(@"^[0-9]+(\.[0-9]+){1,3}(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?\z")]
    private static partial Regex ExactVersionPattern();
}
