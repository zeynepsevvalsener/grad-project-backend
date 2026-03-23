namespace GradProject.Domain.Entities;

/// <summary>
/// H3 cell belonging to a territory. Normalized alternative to legacy <see cref="Territory.GeometryCells"/> JSON.
/// Max length and uniqueness are enforced via Fluent API in AppDbContext.
/// </summary>
public class TerritoryCell
{
    public int Id { get; set; }

    public int TerritoryId { get; set; }

    /// <summary>H3 index string (resolution depends on ingestion).</summary>
    public string H3Index { get; set; } = null!;

    public Territory Territory { get; set; } = null!;
}
