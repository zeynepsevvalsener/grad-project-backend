namespace GradProject.Application.DTOs.Gamification;

/// <summary>
/// Public territory row for GET /api/v1/territory (id is stable <see cref="PublicId" /> for clients).
/// </summary>
public sealed class TerritoryCatalogItemDto
{
    /// <summary>Stable external identifier (maps to domain <c>Territory.PublicId</c>).</summary>
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;
    public string? Description { get; init; }

    public IReadOnlyList<string> GeometryCells { get; init; } = Array.Empty<string>();

    public int? CurrentOwnerUserId { get; init; }
    public string? CurrentOwnerUsername { get; init; }
    public decimal? CurrentOwnerScoreSnapshot { get; init; }

    public int OwnershipTargetPercent { get; init; }
    public bool IsActive { get; init; }
}
