using System.Text.Json;

namespace GradProject.Application.Utilities;

/// <summary>
/// Resolves a territory's H3 cell list, preferring normalized <c>TerritoryCells</c> rows
/// and falling back to the legacy <c>GeometryCells</c> JSONB column.
/// </summary>
public static class TerritoryGeometryResolver
{
    public static IReadOnlyList<string> Resolve(IReadOnlyList<string> tableCells, string? legacyJson)
    {
        if (tableCells.Count > 0)
            return tableCells;

        return ParseLegacyJson(legacyJson);
    }

    private static IReadOnlyList<string> ParseLegacyJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return Array.Empty<string>();

            return doc.RootElement.EnumerateArray()
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .Distinct()
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
