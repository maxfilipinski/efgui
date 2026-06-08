using EfGui.Engine;

namespace EfGui.Tests;

public class MigrationNameTests
{
    [Theory]
    [InlineData("AddUsers")]
    [InlineData("Add_Users_2")]
    [InlineData("_Init")]
    [InlineData("  Trimmed  ")]
    public void Accepts_valid_identifiers(string name) =>
        Assert.True(MigrationName.IsValid(name));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("Add Users")]
    [InlineData("2Cool")]
    [InlineData("Add-Users")]
    [InlineData("Add.Users")]
    public void Rejects_invalid_names(string? name) =>
        Assert.False(MigrationName.IsValid(name));
}
