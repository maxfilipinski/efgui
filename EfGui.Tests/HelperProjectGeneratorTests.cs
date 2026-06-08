using EfGui.Engine;
using EfGui.Profiles;

namespace EfGui.Tests;

public class HelperProjectGeneratorTests
{
    private static Profile BaseProfile() => new()
    {
        Name = "Test",
        CsprojPath = @"C:\proj\MyApp\MyApp.csproj",
        DbContextName = "MyApp.Data.AppDbContext",
        MigrationsDir = "Migrations",
        TargetFramework = "net10.0",
        EfCoreDesignVersion = "10.0.8",
        DbConfigMode = DbConfigMode.ConnectionString,
        DbProvider = DbProvider.Sqlite,
        ConnectionString = "Data Source=app.db"
    };

    [Fact]
    public void Csproj_references_design_provider_and_target_project()
    {
        var (csproj, _) = HelperProjectGenerator.BuildSources(BaseProfile());

        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", csproj);
        Assert.Contains("Microsoft.EntityFrameworkCore.Design\" Version=\"10.0.8\"", csproj);
        Assert.Contains("Microsoft.EntityFrameworkCore.Sqlite", csproj);
        Assert.Contains(@"C:\proj\MyApp\MyApp.csproj", csproj);
    }

    [Fact]
    public void Factory_implements_design_time_factory_for_context()
    {
        var (_, factory) = HelperProjectGenerator.BuildSources(BaseProfile());

        Assert.Contains("IDesignTimeDbContextFactory<MyApp.Data.AppDbContext>", factory);
        Assert.Contains("optionsBuilder.UseSqlite(", factory);
    }

    [Fact]
    public void Connection_string_is_emitted_as_verbatim_literal_with_escaped_quotes()
    {
        var profile = BaseProfile();
        profile.ConnectionString = "Server=.;Password=\"p\"";

        var (_, factory) = HelperProjectGenerator.BuildSources(profile);

        // Verbatim string with doubled quotes: @"Server=.;Password=""p"""
        Assert.Contains("@\"Server=.;Password=\"\"p\"\"\"", factory);
    }

    [Fact]
    public void Independent_provider_version_used_for_mysql_when_unpinned()
    {
        var profile = BaseProfile();
        profile.DbProvider = DbProvider.MySql;
        profile.ProviderPackageVersion = "";

        var (csproj, _) = HelperProjectGenerator.BuildSources(profile);

        Assert.Contains("Pomelo.EntityFrameworkCore.MySql", csproj);
        // Falls back to the provider's independent default, not the EF Core version.
        Assert.DoesNotContain("Pomelo.EntityFrameworkCore.MySql\" Version=\"10.0.8\"", csproj);
    }

    [Fact]
    public void Custom_code_mode_omits_provider_reference_and_inlines_code()
    {
        var profile = BaseProfile();
        profile.DbConfigMode = DbConfigMode.CustomCode;
        profile.CustomCode = "optionsBuilder.UseNpgsql(\"Host=localhost\");";

        var (csproj, factory) = HelperProjectGenerator.BuildSources(profile);

        Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Sqlite", csproj);
        Assert.Contains("optionsBuilder.UseNpgsql(\"Host=localhost\");", factory);
    }
}
