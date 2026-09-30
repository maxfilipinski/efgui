namespace EfGui.Core.Engine;

// A migration name becomes a C# class name, so it must be a valid identifier.
public static class MigrationName
{
    public static bool IsValid(string? name) => CSharpIdentifier.IsValid(name);
}
