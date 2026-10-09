using System.Text.Json;

namespace EfGui.Core.Migrations;

// Parses `dotnet ef migrations list --json --prefix-output`: the JSON is the "data:" lines
// joined, which, unlike the human-readable listing, doesn't depend on locale.
public static class MigrationListParser
{
    private const string DataPrefix = "data:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // "applied" is null when listed with --no-connect; treat unknown as not applied.
    private sealed record Dto(string? Id, string? Name, bool? Applied);

    // Null when unparseable; empty when there are no migrations.
    public static IReadOnlyList<MigrationInfo>? Parse(IEnumerable<string> stdoutLines)
    {
        var json = ExtractJson(stdoutLines);
        if (json is null)
        {
            return null;
        }

        try
        {
            var dtos = JsonSerializer.Deserialize<List<Dto>>(json, JsonOptions);
            if (dtos is null)
            {
                return null;
            }

            return dtos
                .Where(dto => !string.IsNullOrEmpty(dto.Id))
                .Select(dto => new MigrationInfo(dto.Id!, dto.Name ?? dto.Id!, dto.Applied ?? false))
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
            '\n',
            list.Where(line => line.StartsWith(DataPrefix, StringComparison.Ordinal))
                .Select(line => line[DataPrefix.Length..]));

        if (!string.IsNullOrWhiteSpace(dataPayload))
        {
            return dataPayload.Trim();
        }

        // Fallback for output without --prefix-output: isolate the JSON array.
        var all = string.Join('\n', list);
        var start = all.IndexOf('[');
        var end = all.LastIndexOf(']');
        return start >= 0 && end > start ? all[start..(end + 1)] : null;
    }
}
