namespace EfGui.Core.Migrations;

public static class MigrationScriptRange
{
    public const string Start = "0";

    public static string PreviousId(IReadOnlyList<MigrationInfo> migrations) =>
        migrations.Count >= 2 ? migrations[^2].Id : Start;

    public static string LastId(IReadOnlyList<MigrationInfo> migrations) =>
        migrations[^1].Id;

    public static string LastAppliedId(IReadOnlyList<MigrationInfo> migrations) =>
        migrations.LastOrDefault(migration => migration.Applied)?.Id ?? Start;

    public static bool AnyUnapplied(IReadOnlyList<MigrationInfo> migrations) =>
        migrations.Any(migration => !migration.Applied);
}
