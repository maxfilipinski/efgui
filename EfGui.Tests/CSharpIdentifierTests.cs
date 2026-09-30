using EfGui.Core.Engine;

namespace EfGui.Tests;

public class CSharpIdentifierTests
{
    [Theory]
    [InlineData("AppDbContext")]
    [InlineData("MyApp.Data.AppDbContext")]
    [InlineData("_Internal.Ctx")]
    public void Accepts_qualified_names(string name) =>
        Assert.True(CSharpIdentifier.IsValidQualified(name));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("MyApp..Ctx")]
    [InlineData("MyApp.Ctx.")]
    [InlineData(".Ctx")]
    [InlineData("MyApp.2Ctx")]
    [InlineData("MyApp.Ctx<T>")]
    public void Rejects_invalid_qualified_names(string? name) =>
        Assert.False(CSharpIdentifier.IsValidQualified(name));
}
