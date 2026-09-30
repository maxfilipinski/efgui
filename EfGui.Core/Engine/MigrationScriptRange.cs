namespace EfGui.Core.Engine;

// Pure helpers translating a migration list into the [from] [to] arguments
// `dotnet ef migrations script` expects. "0" is EF's sentinel for "before the
// first migration".
public static class MigrationScriptRange
{
    public const string Start = "0";

    public static string PreviousId(IReadOnlyList<MigrationInfo> migrations) =>
        migrations.Count >= 2 ? migrations[^2].Id : Start;

    public static string LastId(IReadOnlyList<MigrationInfo> migrations) =>
        migrations[^1].Id;

    public static string LastAppliedId(IReadOnlyList<MigrationInfo> migrations) =>
        migrations.LastOrDefault(m => m.Applied)?.Id ?? Start;

    public static bool AnyUnapplied(IReadOnlyList<MigrationInfo> migrations) =>
        migrations.Any(m => !m.Applied);
}
