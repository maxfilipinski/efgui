using System.Globalization;

namespace EfGui.Core.Profiles;

public sealed class DbProviderInfo
{
    public required DbProvider Provider { get; init; }
    public required string DisplayName { get; init; }
    public required string PackageId { get; init; }

    public string? IndependentDefaultVersion { get; init; }

    public required string ConfigureStatementFormat { get; init; }

    public string GetConfigureStatement(string connectionStringLiteral) =>
        string.Format(CultureInfo.InvariantCulture, ConfigureStatementFormat, connectionStringLiteral);

    public static readonly IReadOnlyList<DbProviderInfo> All =
    [
        new()
        {
            Provider = DbProvider.SqlServer,
            DisplayName = "SQL Server",
            PackageId = "Microsoft.EntityFrameworkCore.SqlServer",
            ConfigureStatementFormat = "optionsBuilder.UseSqlServer({0});"
        },
        new()
        {
            Provider = DbProvider.PostgreSql,
            DisplayName = "PostgreSQL",
            PackageId = "Npgsql.EntityFrameworkCore.PostgreSQL",
            ConfigureStatementFormat = "optionsBuilder.UseNpgsql({0});"
        },
        new()
        {
            Provider = DbProvider.Sqlite,
            DisplayName = "SQLite",
            PackageId = "Microsoft.EntityFrameworkCore.Sqlite",
            ConfigureStatementFormat = "optionsBuilder.UseSqlite({0});"
        },
        new()
        {
            Provider = DbProvider.MySql,
            DisplayName = "MySQL",
            PackageId = "Pomelo.EntityFrameworkCore.MySql",
            IndependentDefaultVersion = "9.0.0",
            ConfigureStatementFormat = "optionsBuilder.UseMySql({0}, Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect({0}));"
        }
    ];

    public static DbProviderInfo Get(DbProvider provider)
    {
        return All.FirstOrDefault(info => info.Provider == provider)
               ?? throw new KeyNotFoundException($"Unknown provider: {provider}");
    }
}
