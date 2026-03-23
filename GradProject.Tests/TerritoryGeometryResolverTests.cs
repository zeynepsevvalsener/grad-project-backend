using GradProject.Application.Utilities;

namespace GradProject.Tests;

public class TerritoryGeometryResolverTests
{
    // ────────────────────────────────────────────────
    // Table cells present — always wins
    // ────────────────────────────────────────────────

    [Fact]
    public void Resolve_TableCellsNotEmpty_ReturnsTableCells()
    {
        var tableCells = new List<string> { "abc", "def" };

        var result = TerritoryGeometryResolver.Resolve(tableCells, "[\"legacy\"]");

        Assert.Equal(new[] { "abc", "def" }, result);
    }

    [Fact]
    public void Resolve_TableCellsNotEmpty_IgnoresNullJson()
    {
        var tableCells = new List<string> { "abc" };

        var result = TerritoryGeometryResolver.Resolve(tableCells, null);

        Assert.Equal(new[] { "abc" }, result);
    }

    // ────────────────────────────────────────────────
    // Legacy JSON fallback
    // ────────────────────────────────────────────────

    [Fact]
    public void Resolve_EmptyTableCells_ValidJsonArray_ParsesJson()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), "[\"h3a\",\"h3b\"]");

        Assert.Equal(2, result.Count);
        Assert.Contains("h3a", result);
        Assert.Contains("h3b", result);
    }

    [Fact]
    public void Resolve_EmptyTableCells_NullJson_ReturnsEmpty()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), null);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_EmptyTableCells_EmptyStringJson_ReturnsEmpty()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), "");

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_EmptyTableCells_WhitespaceJson_ReturnsEmpty()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), "   ");

        Assert.Empty(result);
    }

    // ────────────────────────────────────────────────
    // Robustness: malformed / unexpected JSON shapes
    // ────────────────────────────────────────────────

    [Fact]
    public void Resolve_EmptyTableCells_JsonObject_ReturnsEmpty()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), "{\"key\":\"value\"}");

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_EmptyTableCells_MalformedJson_ReturnsEmpty_NoException()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), "not-valid-json");

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_EmptyTableCells_EmptyJsonArray_ReturnsEmpty()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), "[]");

        Assert.Empty(result);
    }

    // ────────────────────────────────────────────────
    // Deduplication
    // ────────────────────────────────────────────────

    [Fact]
    public void Resolve_EmptyTableCells_JsonArrayWithDuplicates_Deduplicates()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), "[\"h3a\",\"h3a\",\"h3b\"]");

        Assert.Equal(2, result.Count);
        Assert.Contains("h3a", result);
        Assert.Contains("h3b", result);
    }

    // ────────────────────────────────────────────────
    // Null entries in JSON array
    // ────────────────────────────────────────────────

    [Fact]
    public void Resolve_EmptyTableCells_JsonArrayWithNullEntries_FiltersNulls()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), "[\"h3a\",null,\"h3b\"]");

        Assert.Equal(2, result.Count);
        Assert.All(result, cell => Assert.NotNull(cell));
    }

    [Fact]
    public void Resolve_EmptyTableCells_JsonArrayWithWhitespaceEntries_FiltersWhitespace()
    {
        var result = TerritoryGeometryResolver.Resolve(Array.Empty<string>(), "[\"h3a\",\" \",\"h3b\"]");

        // whitespace-only strings are filtered
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(" ", result);
    }
}
