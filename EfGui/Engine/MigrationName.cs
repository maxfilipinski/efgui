using System.Text.RegularExpressions;

namespace EfGui.Engine;

// A migration name becomes a C# class name, so it must be a valid identifier.
public static partial class MigrationName
{
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierRegex();

    public static bool IsValid(string? name) =>
        !string.IsNullOrWhiteSpace(name) && IdentifierRegex().IsMatch(name.Trim());
}
