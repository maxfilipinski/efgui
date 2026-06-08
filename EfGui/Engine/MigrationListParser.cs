using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EfGui.Engine;

public record MigrationInfo(string Id, string Name, bool Applied);

// Parses `dotnet ef migrations list --json --prefix-output`. With --prefix-output
// each line is tagged ("data:", "info:", "warn:"...); the JSON payload is the
// concatenation of the "data:" lines. This is locale-independent, unlike the
// human-readable listing.
public static class MigrationListParser
{
    private const string DataPrefix = "data:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // "applied" is null when listed with --no-connect; treat unknown as not applied.
    private record Dto(string? Id, string? Name, bool? Applied);

    // Returns null when the output could not be parsed at all; an empty list means
    // the command succeeded but reported no migrations.
    public static IReadOnlyList<MigrationInfo>? Parse(IEnumerable<string> stdoutLines)
    {
        var json = ExtractJson(stdoutLines);
        if (json is null)
            return null;

        try
        {
            var dtos = JsonSerializer.Deserialize<List<Dto>>(json, JsonOptions);
            if (dtos is null)
                return null;

            return dtos
                .Where(d => !string.IsNullOrEmpty(d.Id))
                .Select(d => new MigrationInfo(d.Id!, d.Name ?? d.Id!, d.Applied ?? false))
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractJson(IEnumerable<string> lines)
    {
        var list = lines as IList<string> ?? lines.ToList();

        var dataPayload = string.Join(
            "\n",
            list.Where(l => l.StartsWith(DataPrefix, StringComparison.Ordinal))
                .Select(l => l[DataPrefix.Length..]));

        if (!string.IsNullOrWhiteSpace(dataPayload))
            return dataPayload.Trim();

        // Fallback for output without --prefix-output: isolate the JSON array.
        var all = string.Join("\n", list);
        var start = all.IndexOf('[');
        var end = all.LastIndexOf(']');
        return start >= 0 && end > start ? all[start..(end + 1)] : null;
    }
}

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
