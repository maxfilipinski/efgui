using EfGui.Engine;

namespace EfGui.Tests;

public class MigrationScriptRangeTests
{
    private static IReadOnlyList<MigrationInfo> Migrations(params (string Id, bool Applied)[] items) =>
        items.Select(i => new MigrationInfo(i.Id, i.Id, i.Applied)).ToList();

    [Fact]
    public void Previous_is_sentinel_when_only_one_migration()
    {
        var m = Migrations(("1_A", false));
        Assert.Equal(MigrationScriptRange.Start, MigrationScriptRange.PreviousId(m));
    }

    [Fact]
    public void Previous_is_second_to_last()
    {
        var m = Migrations(("1_A", true), ("2_B", true), ("3_C", false));
        Assert.Equal("2_B", MigrationScriptRange.PreviousId(m));
    }

    [Fact]
    public void Last_is_final_entry()
    {
        var m = Migrations(("1_A", true), ("2_B", false));
        Assert.Equal("2_B", MigrationScriptRange.LastId(m));
    }

    [Fact]
    public void LastApplied_is_sentinel_when_none_applied()
    {
        var m = Migrations(("1_A", false), ("2_B", false));
        Assert.Equal(MigrationScriptRange.Start, MigrationScriptRange.LastAppliedId(m));
    }

    [Fact]
    public void LastApplied_finds_highest_applied()
    {
        var m = Migrations(("1_A", true), ("2_B", true), ("3_C", false));
        Assert.Equal("2_B", MigrationScriptRange.LastAppliedId(m));
    }

    [Theory]
    [InlineData(true, true, false)]   // all applied -> nothing unapplied
    [InlineData(true, false, true)]   // one pending -> unapplied
    [InlineData(false, false, true)]  // none applied -> unapplied
    public void AnyUnapplied_reflects_flags(bool a, bool b, bool expectedAnyUnapplied)
    {
        var m = Migrations(("1_A", a), ("2_B", b));
        Assert.Equal(expectedAnyUnapplied, MigrationScriptRange.AnyUnapplied(m));
    }
}
