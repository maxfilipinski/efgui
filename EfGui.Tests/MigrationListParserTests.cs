using EfGui.Engine;

namespace EfGui.Tests;

public class MigrationListParserTests
{
    [Fact]
    public void Parses_prefix_output_json()
    {
        var lines = new[]
        {
            "info:   Using project 'X'",
            "data:   [",
            "data:     { \"id\": \"20240101000000_Init\", \"name\": \"Init\", \"applied\": true },",
            "data:     { \"id\": \"20240102000000_AddUsers\", \"name\": \"AddUsers\", \"applied\": false }",
            "data:   ]"
        };

        var result = MigrationListParser.Parse(lines);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("20240101000000_Init", result[0].Id);
        Assert.True(result[0].Applied);
        Assert.Equal("AddUsers", result[1].Name);
        Assert.False(result[1].Applied);
    }

    [Fact]
    public void Falls_back_to_raw_json_without_prefix()
    {
        var lines = new[]
        {
            "Build succeeded.",
            "[{ \"id\": \"20240101000000_Init\", \"name\": \"Init\", \"applied\": false }]"
        };

        var result = MigrationListParser.Parse(lines);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("20240101000000_Init", result![0].Id);
    }

    [Fact]
    public void Empty_array_is_empty_list_not_null()
    {
        var result = MigrationListParser.Parse(new[] { "data: []" });

        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public void Returns_null_when_no_json_present()
    {
        var result = MigrationListParser.Parse(new[] { "info: building", "Build succeeded." });

        Assert.Null(result);
    }

    [Fact]
    public void Treats_null_applied_as_not_applied()
    {
        // Real --json --no-connect output: applied is null when EF didn't query the DB.
        var lines = new[]
        {
            "data:    [",
            "data:      {",
            "data:        \"id\": \"20260608121718_Init\",",
            "data:        \"name\": \"Init\",",
            "data:        \"safeName\": \"Init\",",
            "data:        \"applied\": null",
            "data:      }",
            "data:    ]"
        };

        var result = MigrationListParser.Parse(lines);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("20260608121718_Init", result![0].Id);
        Assert.False(result[0].Applied);
    }

    [Fact]
    public void Is_case_insensitive_on_property_names()
    {
        var result = MigrationListParser.Parse(new[]
        {
            "data: [{ \"Id\": \"20240101000000_Init\", \"Name\": \"Init\", \"Applied\": true }]"
        });

        Assert.NotNull(result);
        Assert.True(result![0].Applied);
    }
}
