using System.Collections.Generic;

namespace EfGui.Profiles;

public class DbProviderInfo
{
    public required DbProvider Provider { get; init; }
    public required string DisplayName { get; init; }
    public required string PackageId { get; init; }

    // Fallback for providers versioned independently of EF Core.
    public string? IndependentDefaultVersion { get; init; }

    // optionsBuilder statement emitted into the helper project factory; {0} = connection string literal.
    public required string ConfigureStatementFormat { get; init; }

    public string GetConfigureStatement(string connectionStringLiteral) =>
        string.Format(ConfigureStatementFormat, connectionStringLiteral);

    public static readonly IReadOnlyList<DbProviderInfo> All = new[]
    {
        new DbProviderInfo
        {
            Provider = DbProvider.SqlServer,
            DisplayName = "SQL Server",
            PackageId = "Microsoft.EntityFrameworkCore.SqlServer",
            ConfigureStatementFormat = "optionsBuilder.UseSqlServer({0});"
        },
        new DbProviderInfo
        {
            Provider = DbProvider.PostgreSql,
            DisplayName = "PostgreSQL",
            PackageId = "Npgsql.EntityFrameworkCore.PostgreSQL",
            ConfigureStatementFormat = "optionsBuilder.UseNpgsql({0});"
        },
        new DbProviderInfo
        {
            Provider = DbProvider.Sqlite,
            DisplayName = "SQLite",
            PackageId = "Microsoft.EntityFrameworkCore.Sqlite",
            ConfigureStatementFormat = "optionsBuilder.UseSqlite({0});"
        },
        new DbProviderInfo
        {
            Provider = DbProvider.MySql,
            DisplayName = "MySQL",
            PackageId = "Pomelo.EntityFrameworkCore.MySql",
            // Pomelo releases independently of EF Core and usually lags behind.
            IndependentDefaultVersion = "9.0.0",
            ConfigureStatementFormat = "optionsBuilder.UseMySql({0}, Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect({0}));"
        }
    };

    public static DbProviderInfo Get(DbProvider provider)
    {
        foreach (var info in All)
        {
            if (info.Provider == provider)
                return info;
        }

        throw new KeyNotFoundException($"Unknown provider: {provider}");
    }
}
