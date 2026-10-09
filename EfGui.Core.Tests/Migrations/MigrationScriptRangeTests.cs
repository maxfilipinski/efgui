using EfGui.Core.Migrations;

namespace EfGui.Core.Tests.Migrations;

public class MigrationScriptRangeTests
{
    private static List<MigrationInfo> Migrations(params (string Id, bool Applied)[] items) =>
        items.Select(item => new MigrationInfo(item.Id, item.Id, item.Applied)).ToList();

    [Fact]
    public void Previous_is_sentinel_when_only_one_migration()
    {
        var migrations = Migrations(("1_A", false));
        Assert.Equal(MigrationScriptRange.Start, MigrationScriptRange.PreviousId(migrations));
    }

    [Fact]
    public void Previous_is_second_to_last()
    {
        var migrations = Migrations(("1_A", true), ("2_B", true), ("3_C", false));
        Assert.Equal("2_B", MigrationScriptRange.PreviousId(migrations));
    }

    [Fact]
    public void Last_is_final_entry()
    {
        var migrations = Migrations(("1_A", true), ("2_B", false));
        Assert.Equal("2_B", MigrationScriptRange.LastId(migrations));
    }

    [Fact]
    public void LastApplied_is_sentinel_when_none_applied()
    {
        var migrations = Migrations(("1_A", false), ("2_B", false));
        Assert.Equal(MigrationScriptRange.Start, MigrationScriptRange.LastAppliedId(migrations));
    }

    [Fact]
    public void LastApplied_finds_highest_applied()
    {
        var migrations = Migrations(("1_A", true), ("2_B", true), ("3_C", false));
        Assert.Equal("2_B", MigrationScriptRange.LastAppliedId(migrations));
    }

    [Theory]
    [InlineData(true, true, false)]   // all applied -> nothing unapplied
    [InlineData(true, false, true)]   // one pending -> unapplied
    [InlineData(false, false, true)]  // none applied -> unapplied
    public void AnyUnapplied_reflects_flags(bool firstApplied, bool secondApplied, bool expectedAnyUnapplied)
    {
        var migrations = Migrations(("1_A", firstApplied), ("2_B", secondApplied));
        Assert.Equal(expectedAnyUnapplied, MigrationScriptRange.AnyUnapplied(migrations));
    }
}
