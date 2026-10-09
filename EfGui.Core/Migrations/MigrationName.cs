namespace EfGui.Core.Migrations;

public static class MigrationName
{
    public static bool IsValid(string? name) => CSharpIdentifier.IsValid(name);
}
