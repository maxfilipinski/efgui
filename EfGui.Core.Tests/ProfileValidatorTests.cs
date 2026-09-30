using EfGui.Core.Profiles;

namespace EfGui.Core.Tests;

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
        { p => p.Name = " ", "Profile name is required" },
        { p => p.CsprojPath = "", "Project path is required" },
        { p => p.CsprojPath = "MyApp.sln", "must point to a .csproj" },
        { p => p.CsprojPath = Path.Combine(Path.GetTempPath(), "EfGuiTests", "missing.csproj"), "does not exist" },
        { p => p.DbContextName = "", "DbContext class name is required" },
        { p => p.DbContextName = "MyApp.2Ctx", "valid C# type name" },
        { p => p.MigrationsDir = "", "Migrations directory is required" },
        { p => p.TargetFramework = "", "Target framework is required" },
        { p => p.DotnetEfVersion = "", "dotnet-ef version is required" },
        { p => p.EfCoreDesignVersion = "", "EF Core Design version is required" },
        { p => p.ConnectionString = "", "Connection string is required" },
        { p => { p.DbConfigMode = DbConfigMode.CustomCode; p.CustomCode = " "; }, "Configuration code is required" }
    };

    [Theory]
    [MemberData(nameof(InvalidProfiles))]
    public void Invalid_profile_reports_problem(Action<Profile> breakIt, string expected)
    {
        var profile = ValidProfile();
        breakIt(profile);

        Assert.Contains(expected, ProfileValidator.Validate(profile));
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
