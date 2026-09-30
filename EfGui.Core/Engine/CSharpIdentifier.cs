using System.Text.RegularExpressions;

namespace EfGui.Core.Engine;

public static partial class CSharpIdentifier
{
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex SimpleRegex();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex QualifiedRegex();

    public static bool IsValid(string? name) =>
        !string.IsNullOrWhiteSpace(name) && SimpleRegex().IsMatch(name.Trim());

    // Namespace-qualified, e.g. "MyApp.Data.AppDbContext".
    public static bool IsValidQualified(string? name) =>
        !string.IsNullOrWhiteSpace(name) && QualifiedRegex().IsMatch(name.Trim());
}
