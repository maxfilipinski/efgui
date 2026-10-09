using EfGui.Core.Profiles;

namespace EfGui.Core.Tests.Profiles;

public sealed class ProfileValidatorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "EfGuiTests", Guid.NewGuid().ToString("N"));
    private readonly string _csproj;

    public ProfileValidatorTests()
    {
        Directory.CreateDirectory(_dir);
        _csproj = Path.Combine(_dir, "MyApp.csproj");
        File.WriteAllText(_csproj, "<Project />");
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private Profile ValidProfile() => new()
    {
        Name = "P",
        CsprojPath = _csproj,
        DbContextName = "MyApp.Data.AppDbContext",
        ConnectionString = "Data Source=app.db"
    };

    [Fact]
    public void Valid_profile_passes() =>
        Assert.Null(ProfileValidator.Validate(ValidProfile()));

    [Fact]
    public void Csproj_path_is_trimmed_before_checks()
    {
        var profile = ValidProfile();
        profile.CsprojPath = "  " + _csproj + "  ";

        Assert.Null(ProfileValidator.Validate(profile));
    }

    public static TheoryData<Action<Profile>, string> InvalidProfiles => new()
    {
        { profile => profile.Name = " ", "Profile name is required" },
        { profile => profile.CsprojPath = "", "Project path is required" },
        { profile => profile.CsprojPath = "MyApp.sln", "must point to a .csproj" },
        { profile => profile.CsprojPath = Path.Combine(Path.GetTempPath(), "EfGuiTests", "missing.csproj"), "does not exist" },
        { profile => profile.DbContextName = "", "DbContext class name is required" },
        { profile => profile.DbContextName = "MyApp.2Ctx", "valid C# type name" },
        { profile => profile.MigrationsDir = "", "Migrations directory is required" },
        { profile => profile.TargetFramework = "", "Target framework is required" },
        { profile => profile.DotnetEfVersion = "", "dotnet-ef version is required" },
        { profile => profile.DotnetEfVersion = "10.*", "dotnet-ef version must be an exact version" },
        { profile => profile.DotnetEfVersion = @"..\..\evil", "dotnet-ef version must be an exact version" },
        { profile => profile.DotnetEfVersion = "10.0.8\n", "dotnet-ef version must be an exact version" },
        { profile => profile.EfCoreDesignVersion = "", "EF Core Design version is required" },
        { profile => profile.EfCoreDesignVersion = "10.0.8\"/>", "EF Core Design version must be an exact version" },
        { profile => profile.ProviderPackageVersion = "latest", "Provider package version must be an exact version" },
        { profile => profile.DbConfigMode = (DbConfigMode)7, "Unknown database configuration mode" },
        { profile => profile.DbProvider = (DbProvider)7, "Unknown database provider" },
        { profile => profile.ConnectionString = "", "Connection string is required" },
        { profile => { profile.DbConfigMode = DbConfigMode.CustomCode; profile.CustomCode = " "; }, "Configuration code is required" }
    };

    [Theory]
    [MemberData(nameof(InvalidProfiles))]
    public void Invalid_profile_reports_problem(Action<Profile> breakIt, string expected)
    {
        var profile = ValidProfile();
        breakIt(profile);

        Assert.Contains(expected, ProfileValidator.Validate(profile));
    }

    [Theory]
    [InlineData("10.0.8")]
    [InlineData("9.0")]
    [InlineData("10.0.0-preview.7.25380.108")]
    [InlineData("9.0.0-rc.1+build.42")]
    public void Exact_versions_pass(string version)
    {
        var profile = ValidProfile();
        profile.DotnetEfVersion = version;
        profile.EfCoreDesignVersion = version;
        profile.ProviderPackageVersion = version;

        Assert.Null(ProfileValidator.Validate(profile));
    }

    [Fact]
    public void Connection_string_is_not_required_in_custom_code_mode()
    {
        var profile = ValidProfile();
        profile.DbConfigMode = DbConfigMode.CustomCode;
        profile.ConnectionString = "";
        profile.CustomCode = "optionsBuilder.UseSqlite(\"Data Source=app.db\");";

        Assert.Null(ProfileValidator.Validate(profile));
    }
}
