using EfGui.Core.Profiles;
using System.Security;

namespace EfGui.Core.DotnetEf;

// Generates a startup project with the EF Design package and an IDesignTimeDbContextFactory,
// so the target project needs neither.
public static class HelperProjectGenerator
{
    public const string ConnectionStringVariable = "EFGUI_CONNECTION_STRING";

    public static string Generate(Profile profile)
    {
        var dir = AppPaths.HelperDir(profile.Id);
        Directory.CreateDirectory(dir);

        var (csproj, factory) = BuildSources(profile);
        var csprojPath = Path.Combine(dir, "EfGuiHelper.csproj");
        WriteIfChanged(csprojPath, csproj);
        WriteIfChanged(Path.Combine(dir, "DesignTimeFactory.cs"), factory);

        return csprojPath;
    }

    public static void Delete(Guid profileId)
    {
        var dir = AppPaths.HelperDir(profileId);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    public static (string Csproj, string Factory) BuildSources(Profile profile)
    {
        return (BuildCsproj(profile), BuildFactory(profile));
    }

    private static string BuildCsproj(Profile profile)
    {
        return $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{Xml(profile.TargetFramework)}</TargetFramework>
                    <OutputType>Library</OutputType>
                    <Nullable>disable</Nullable>
                    <ImplicitUsings>disable</ImplicitUsings>
                    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="{Xml(profile.EfCoreDesignVersion)}" />
                    {ProviderPackageReference(profile)}
                  </ItemGroup>
                  <ItemGroup>
                    <ProjectReference Include="{Xml(profile.CsprojPath.Trim())}" />
                  </ItemGroup>
                </Project>
                """;
    }

    private static string ProviderPackageReference(Profile profile)
    {
        if (profile.DbConfigMode != DbConfigMode.ConnectionString)
        {
            return "";
        }

        var provider = DbProviderInfo.Get(profile.DbProvider);
        var version = !string.IsNullOrWhiteSpace(profile.ProviderPackageVersion)
            ? profile.ProviderPackageVersion
            : provider.IndependentDefaultVersion ?? profile.EfCoreDesignVersion;
        return $"""<PackageReference Include="{Xml(provider.PackageId)}" Version="{Xml(version)}" />""";
    }

    private static string BuildFactory(Profile profile)
    {
        var context = profile.DbContextName;

        var configuration = profile.DbConfigMode == DbConfigMode.CustomCode
            ? IndentLines(profile.CustomCode, "            ")
            : $$"""
                            var connectionString = System.Environment.GetEnvironmentVariable("{{ConnectionStringVariable}}")
                                ?? throw new System.InvalidOperationException("{{ConnectionStringVariable}} is not set.");
                            {{DbProviderInfo.Get(profile.DbProvider).GetConfigureStatement("connectionString")}}
                """;

        return $$"""
                 using Microsoft.EntityFrameworkCore;
                 using Microsoft.EntityFrameworkCore.Design;

                 namespace EfGuiHelper
                 {
                     public class DesignTimeFactory : IDesignTimeDbContextFactory<{{context}}>
                     {
                         public {{context}} CreateDbContext(string[] args)
                         {
                             var optionsBuilder = new DbContextOptionsBuilder<{{context}}>();
                 {{configuration}}
                             return new {{context}}(optionsBuilder.Options);
                         }
                     }
                 }
                 """;
    }

    private static void WriteIfChanged(string path, string content)
    {
        if (File.Exists(path) && File.ReadAllText(path) == content)
        {
            return;
        }

        File.WriteAllText(path, content);
    }

    private static string Xml(string value)
    {
        return SecurityElement.Escape(value);
    }

    private static string IndentLines(string code, string indent)
    {
        return string.Join('\n', code.ReplaceLineEndings("\n").Split('\n').Select(line => indent + line));
    }
}
