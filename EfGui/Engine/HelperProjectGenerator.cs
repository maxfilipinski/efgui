using EfGui.Profiles;
using System;
using System.IO;
using System.Text;

namespace EfGui.Engine;

// Generates the temporary project that gives dotnet-ef a startup project with the
// EF Design package and an IDesignTimeDbContextFactory, so the target project
// needs neither.
public static class HelperProjectGenerator
{
    public static string Generate(Profile profile)
    {
        // Stable per profile so the helper's obj/bin stay warm between runs.
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EfGui", "helpers", profile.Id.ToString("N"));
        Directory.CreateDirectory(dir);

        var csprojPath = Path.Combine(dir, "EfGuiHelper.csproj");
        File.WriteAllText(csprojPath, BuildCsproj(profile));
        File.WriteAllText(Path.Combine(dir, "DesignTimeFactory.cs"), BuildFactory(profile));

        return csprojPath;
    }

    private static string BuildCsproj(Profile profile)
    {
        var providerReference = "";
        if (profile.DbConfigMode == DbConfigMode.ConnectionString)
        {
            var provider = DbProviderInfo.Get(profile.DbProvider);
            var version = !string.IsNullOrWhiteSpace(profile.ProviderPackageVersion)
                ? profile.ProviderPackageVersion
                : provider.IndependentDefaultVersion ?? profile.EfCoreDesignVersion;
            providerReference =
                $"""    <PackageReference Include="{provider.PackageId}" Version="{version}" />""" + "\n";
        }
        // In CustomCode mode the provider package is expected to flow transitively
        // from the referenced project.

        return $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{profile.TargetFramework}</TargetFramework>
                <OutputType>Library</OutputType>
                <Nullable>disable</Nullable>
                <ImplicitUsings>disable</ImplicitUsings>
                <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="{profile.EfCoreDesignVersion}" />
            {providerReference}  </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="{profile.CsprojPath}" />
              </ItemGroup>
            </Project>
            """;
    }

    private static string BuildFactory(Profile profile)
    {
        var context = profile.DbContextName;

        var configuration = profile.DbConfigMode == DbConfigMode.CustomCode
            ? IndentLines(profile.CustomCode, "            ")
            : "            " + DbProviderInfo.Get(profile.DbProvider)
                .GetConfigureStatement(ToVerbatimLiteral(profile.ConnectionString));

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

    private static string ToVerbatimLiteral(string value) =>
        "@\"" + value.Replace("\"", "\"\"") + "\"";

    private static string IndentLines(string code, string indent)
    {
        var builder = new StringBuilder();
        foreach (var line in code.Replace("\r\n", "\n").Split('\n'))
            builder.Append(indent).Append(line).Append('\n');
        return builder.ToString().TrimEnd('\n');
    }
}
